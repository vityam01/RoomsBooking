using ConferenceRoomApi.Domain.Bookings;
using ConferenceRoomApi.Domain.Common.Exceptions;
using ConferenceRoomApi.Domain.Pricing;
using FluentAssertions;
using Xunit;

namespace ConferenceRoomApi.UnitTests.Bookings;

public class BookingTests
{
    private static readonly RoomCostBreakdown SampleBreakdown = new(
        new[] { new PriceSegment(RateZoneType.Standard, new TimeOnly(10, 0), new TimeOnly(11, 0), 1m, 1m, 2000m, 2000m) },
        2000m);

    [Fact]
    public void Create_SumsRoomCostAndSelectedServices_IntoTotalCost()
    {
        var services = new[]
        {
            new SelectedServiceSnapshot(Guid.NewGuid(), "Projector", 500m),
            new SelectedServiceSnapshot(Guid.NewGuid(), "Wi-Fi", 300m)
        };
        var today = new DateOnly(2026, 9, 1);

        var booking = Booking.Create(Guid.NewGuid(), today, new TimeOnly(10, 0), new TimeOnly(11, 0), today, SampleBreakdown, services);

        booking.RoomCost.Should().Be(2000m);
        booking.ServicesCost.Should().Be(800m);
        booking.TotalCost.Should().Be(2800m);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.BookedServices.Should().HaveCount(2);
    }

    [Fact]
    public void Create_DateInThePast_Throws()
    {
        var today = new DateOnly(2026, 9, 2);
        var yesterday = today.AddDays(-1);

        var act = () => Booking.Create(
            Guid.NewGuid(), yesterday, new TimeOnly(10, 0), new TimeOnly(11, 0), today, SampleBreakdown, Array.Empty<SelectedServiceSnapshot>());

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Cancel_ConfirmedBooking_SetsStatusAndTimestamp()
    {
        var today = new DateOnly(2026, 9, 1);
        var booking = Booking.Create(
            Guid.NewGuid(), today, new TimeOnly(10, 0), new TimeOnly(11, 0), today, SampleBreakdown, Array.Empty<SelectedServiceSnapshot>());

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_AlreadyCancelledBooking_Throws()
    {
        var today = new DateOnly(2026, 9, 1);
        var booking = Booking.Create(
            Guid.NewGuid(), today, new TimeOnly(10, 0), new TimeOnly(11, 0), today, SampleBreakdown, Array.Empty<SelectedServiceSnapshot>());
        booking.Cancel();

        var act = () => booking.Cancel();

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Theory]
    [InlineData(10, 30, 11, 30, true)]  // overlaps tail
    [InlineData(9, 0, 10, 30, true)]    // overlaps head
    [InlineData(9, 0, 10, 0, false)]    // touches, no overlap (half-open interval)
    [InlineData(11, 0, 12, 0, false)]   // touches, no overlap
    [InlineData(9, 0, 12, 0, true)]     // fully contains
    public void OverlapsWith_UsesHalfOpenIntervalSemantics(int otherStartH, int otherStartM, int otherEndH, int otherEndM, bool expected)
    {
        var today = new DateOnly(2026, 9, 1);
        var booking = Booking.Create(
            Guid.NewGuid(), today, new TimeOnly(10, 0), new TimeOnly(11, 0), today, SampleBreakdown, Array.Empty<SelectedServiceSnapshot>());

        booking.OverlapsWith(new TimeOnly(otherStartH, otherStartM), new TimeOnly(otherEndH, otherEndM)).Should().Be(expected);
    }

    [Fact]
    public void OverlapsWith_CancelledBooking_NeverOverlaps()
    {
        var today = new DateOnly(2026, 9, 1);
        var booking = Booking.Create(
            Guid.NewGuid(), today, new TimeOnly(10, 0), new TimeOnly(11, 0), today, SampleBreakdown, Array.Empty<SelectedServiceSnapshot>());
        booking.Cancel();

        booking.OverlapsWith(new TimeOnly(10, 0), new TimeOnly(11, 0)).Should().BeFalse();
    }
}
