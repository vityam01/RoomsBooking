using ConferenceRoomApi.Application.Pricing;

namespace ConferenceRoomApi.Application.Bookings.Dtos;

public sealed record BookedServiceDto(Guid AdditionalServiceId, string Name, decimal Price);

public sealed record BookingDto(
    Guid Id,
    Guid RoomId,
    string RoomName,
    DateOnly BookingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    decimal RoomCost,
    decimal ServicesCost,
    decimal TotalCost,
    IReadOnlyCollection<PriceSegmentDto> PriceBreakdown,
    IReadOnlyCollection<BookedServiceDto> Services,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt);

public sealed record CreateBookingRequest(Guid RoomId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, List<Guid>? AdditionalServiceIds);

public sealed record BookingListFilter(Guid? RoomId, DateOnly? From, DateOnly? To, bool IncludeCancelled, int Page = 1, int PageSize = 20);
