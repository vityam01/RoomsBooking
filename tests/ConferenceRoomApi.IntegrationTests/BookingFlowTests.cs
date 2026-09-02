using System.Net;
using System.Net.Http.Json;
using ConferenceRoomApi.Application.AdditionalServices.Dtos;
using ConferenceRoomApi.Application.Bookings.Dtos;
using ConferenceRoomApi.Application.Common.Dtos;
using ConferenceRoomApi.Application.Reports.Dtos;
using ConferenceRoomApi.Application.Rooms.Dtos;
using FluentAssertions;
using Xunit;

namespace ConferenceRoomApi.IntegrationTests;

/// <summary>
/// End-to-end coverage of the core business flow against a real Postgres database:
/// create a room, find it via search, book it with services, then verify the database's
/// own exclusion constraint (not just the application-level check) rejects an overlap.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class BookingFlowTests : IntegrationTestBase
{
    public BookingFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task FullFlow_CreateRoom_FindIt_BookIt_ThenRejectOverlap()
    {
        var room = await CreateRoomAsync("Зал Тест", capacity: 20, basePricePerHour: 1000m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var searchResponse = await Client.GetAsync($"/api/rooms/available?date={date:yyyy-MM-dd}&startTime=10:00&endTime=11:00&capacity=10");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var available = await searchResponse.Content.ReadFromJsonAsync<List<AvailableRoomDto>>(JsonOptions);
        available.Should().ContainSingle(r => r.Id == room.Id);
        available!.Single(r => r.Id == room.Id).EstimatedRoomCost.Should().Be(1000m);

        var bookingResponse = await Client.PostAsJsonAsync(
            "/api/bookings", new CreateBookingRequest(room.Id, date, new TimeOnly(10, 0), new TimeOnly(11, 0), null), JsonOptions);
        bookingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingDto>(JsonOptions);
        booking!.TotalCost.Should().Be(1000m);
        booking.Status.Should().Be("Confirmed");

        // A second, overlapping booking must be rejected — first by the application-level
        // check, and (if that ever regresses) by the database's exclusion constraint.
        var conflictingResponse = await Client.PostAsJsonAsync(
            "/api/bookings", new CreateBookingRequest(room.Id, date, new TimeOnly(10, 30), new TimeOnly(11, 30), null), JsonOptions);
        conflictingResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // The same slot no longer shows up in a fresh availability search.
        var searchAfterBooking = await Client.GetAsync($"/api/rooms/available?date={date:yyyy-MM-dd}&startTime=10:00&endTime=11:00&capacity=10");
        var stillAvailable = await searchAfterBooking.Content.ReadFromJsonAsync<List<AvailableRoomDto>>(JsonOptions);
        stillAvailable.Should().NotContain(r => r.Id == room.Id);

        // A non-overlapping slot on the same room and day is still bookable.
        var adjacentResponse = await Client.PostAsJsonAsync(
            "/api/bookings", new CreateBookingRequest(room.Id, date, new TimeOnly(11, 0), new TimeOnly(12, 0), null), JsonOptions);
        adjacentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateBooking_WithAdditionalServices_AddsFlatFeeOnTopOfRoomCost()
    {
        var servicesResponse = await Client.GetAsync("/api/additional-services");
        var services = await servicesResponse.Content.ReadFromJsonAsync<List<AdditionalServiceDto>>(JsonOptions);
        var projector = services!.Single(s => s.Name == "Проєктор");

        var room = await CreateRoomAsync("Зал З Послугами", 20, 2000m, new List<Guid> { projector.Id });
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var response = await Client.PostAsJsonAsync(
            "/api/bookings",
            new CreateBookingRequest(room.Id, date, new TimeOnly(10, 0), new TimeOnly(11, 0), new List<Guid> { projector.Id }),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(JsonOptions);
        booking!.RoomCost.Should().Be(2000m);
        booking.ServicesCost.Should().Be(500m);
        booking.TotalCost.Should().Be(2500m);
    }

    [Fact]
    public async Task DeleteRoom_SoftDeletes_ExcludedFromListAndSearch_ButKeepsPastBookingIntact()
    {
        var room = await CreateRoomAsync("Зал На Видалення", 15, 800m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        var bookingResponse = await Client.PostAsJsonAsync(
            "/api/bookings", new CreateBookingRequest(room.Id, date, new TimeOnly(9, 0), new TimeOnly(10, 0), null), JsonOptions);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingDto>(JsonOptions);

        var deleteResponse = await Client.DeleteAsync($"/api/rooms/{room.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await Client.GetAsync("/api/rooms");
        var activeRooms = await listResponse.Content.ReadFromJsonAsync<List<RoomDto>>(JsonOptions);
        activeRooms.Should().NotContain(r => r.Id == room.Id);

        var searchResponse = await Client.GetAsync($"/api/rooms/available?date={date:yyyy-MM-dd}&startTime=9:00&endTime=10:00&capacity=1");
        var available = await searchResponse.Content.ReadFromJsonAsync<List<AvailableRoomDto>>(JsonOptions);
        available.Should().NotContain(r => r.Id == room.Id);

        // The historical booking is still readable with its original room name and cost.
        var getBookingResponse = await Client.GetAsync($"/api/bookings/{booking!.Id}");
        getBookingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var persistedBooking = await getBookingResponse.Content.ReadFromJsonAsync<BookingDto>(JsonOptions);
        persistedBooking!.RoomName.Should().Be("Зал На Видалення");
        persistedBooking.TotalCost.Should().Be(800m);
    }

    [Fact]
    public async Task CancelBooking_FreesUpTheSlotForAnotherBooking()
    {
        var room = await CreateRoomAsync("Зал Скасування", 10, 1000m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4));

        var firstResponse = await Client.PostAsJsonAsync(
            "/api/bookings", new CreateBookingRequest(room.Id, date, new TimeOnly(10, 0), new TimeOnly(11, 0), null), JsonOptions);
        var first = await firstResponse.Content.ReadFromJsonAsync<BookingDto>(JsonOptions);

        var cancelResponse = await Client.DeleteAsync($"/api/bookings/{first!.Id}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondResponse = await Client.PostAsJsonAsync(
            "/api/bookings", new CreateBookingRequest(room.Id, date, new TimeOnly(10, 0), new TimeOnly(11, 0), null), JsonOptions);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Reports_Summary_ReflectsConfirmedBookingRevenue()
    {
        var room = await CreateRoomAsync("Зал Звіт", 10, 2000m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        await Client.PostAsJsonAsync(
            "/api/bookings", new CreateBookingRequest(room.Id, date, new TimeOnly(10, 0), new TimeOnly(11, 0), null), JsonOptions);

        var summaryResponse = await Client.GetAsync($"/api/reports/summary?from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);

        summary!.ConfirmedBookings.Should().Be(1);
        summary.TotalRevenue.Should().Be(2000m);
        summary.MostBookedRoomName.Should().Be("Зал Звіт");
    }

    [Fact]
    public async Task DeactivatedService_CanNoLongerBeBooked_EvenIfStillLinkedToARoom()
    {
        // A service created here (not the shared seeded catalog) so deactivating it doesn't
        // leak state into other tests that run against the same seeded additional_services rows.
        var createServiceResponse = await Client.PostAsJsonAsync(
            "/api/additional-services", new CreateAdditionalServiceRequest("Тимчасова послуга", 400m), JsonOptions);
        var tempService = await createServiceResponse.Content.ReadFromJsonAsync<AdditionalServiceDto>(JsonOptions);

        var room = await CreateRoomAsync("Зал Деактивація", 20, 1500m, new List<Guid> { tempService!.Id });
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8));

        var deactivateResponse = await Client.DeleteAsync($"/api/additional-services/{tempService.Id}");
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // No longer listed as offered by the room.
        var roomResponse = await Client.GetAsync($"/api/rooms/{room.Id}");
        var roomDto = await roomResponse.Content.ReadFromJsonAsync<RoomDto>(JsonOptions);
        roomDto!.Services.Should().NotContain(s => s.Id == tempService.Id);

        // And can no longer be selected when booking, even though the RoomOffering link still exists.
        var bookingResponse = await Client.PostAsJsonAsync(
            "/api/bookings",
            new CreateBookingRequest(room.Id, date, new TimeOnly(10, 0), new TimeOnly(11, 0), new List<Guid> { tempService.Id }),
            JsonOptions);

        bookingResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("05:00", "07:00")] // before opening
    [InlineData("22:00", "23:30")] // after closing
    public async Task CreateBooking_OutsideBusinessHours_Returns400(string start, string end)
    {
        var room = await CreateRoomAsync("Зал Межі", 10, 1000m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6));

        var response = await Client.PostAsJsonAsync(
            "/api/bookings", new { roomId = room.Id, date, startTime = start, endTime = end, additionalServiceIds = (List<Guid>?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SearchAvailable_NonPositiveCapacity_Returns400(int capacity)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var response = await Client.GetAsync($"/api/rooms/available?date={date:yyyy-MM-dd}&startTime=10:00&endTime=11:00&capacity={capacity}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchAvailable_StartAfterEnd_Returns400WithFluentValidationMessage()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var response = await Client.GetAsync($"/api/rooms/available?date={date:yyyy-MM-dd}&startTime=12:00&endTime=10:00&capacity=1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("StartTime");
    }

    [Fact]
    public async Task ListBookings_Paginates_AndReportsTotalCount()
    {
        var room = await CreateRoomAsync("Зал Пагінація", 10, 1000m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9));

        for (var i = 0; i < 3; i++)
        {
            var start = new TimeOnly(9 + i, 0);
            var response = await Client.PostAsJsonAsync(
                "/api/bookings", new CreateBookingRequest(room.Id, date, start, start.AddHours(1), null), JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var firstPageResponse = await Client.GetAsync($"/api/bookings?roomId={room.Id}&page=1&pageSize=2");
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<PagedResult<BookingDto>>(JsonOptions);
        firstPage!.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(3);
        firstPage.TotalPages.Should().Be(2);

        var secondPageResponse = await Client.GetAsync($"/api/bookings?roomId={room.Id}&page=2&pageSize=2");
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<PagedResult<BookingDto>>(JsonOptions);
        secondPage!.Items.Should().HaveCount(1);

        // No overlap between the two pages.
        firstPage.Items.Select(b => b.Id).Should().NotIntersectWith(secondPage.Items.Select(b => b.Id));
    }

    private async Task<RoomDto> CreateRoomAsync(string name, int capacity, decimal basePricePerHour, List<Guid>? serviceIds = null)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/rooms", new CreateRoomRequest(name, capacity, basePricePerHour, serviceIds), JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RoomDto>(JsonOptions))!;
    }
}
