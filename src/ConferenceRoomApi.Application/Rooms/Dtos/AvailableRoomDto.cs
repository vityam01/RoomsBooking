using ConferenceRoomApi.Application.AdditionalServices.Dtos;
using ConferenceRoomApi.Application.Pricing;

namespace ConferenceRoomApi.Application.Rooms.Dtos;

/// <summary>
/// A room returned by availability search, enriched with a price estimate for the exact
/// slot the client asked about — so they can compare rooms by cost before booking, not just
/// by capacity and base rate.
/// </summary>
public sealed record AvailableRoomDto(
    Guid Id,
    string Name,
    int Capacity,
    decimal BasePricePerHour,
    IReadOnlyCollection<AdditionalServiceDto> Services,
    decimal EstimatedRoomCost,
    IReadOnlyCollection<PriceSegmentDto> PriceBreakdown);

public sealed record SearchAvailableRoomsRequest(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity);
