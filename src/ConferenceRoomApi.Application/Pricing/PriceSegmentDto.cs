using ConferenceRoomApi.Domain.Pricing;

namespace ConferenceRoomApi.Application.Pricing;

/// <summary>API-facing view of one <see cref="PriceSegment"/>, shared by room search (estimate) and booking confirmation (actual).</summary>
public sealed record PriceSegmentDto(
    RateZoneType Zone,
    TimeOnly Start,
    TimeOnly End,
    decimal Hours,
    decimal Multiplier,
    decimal RatePerHour,
    decimal SegmentCost)
{
    public static PriceSegmentDto FromDomain(PriceSegment segment)
        => new(segment.Zone, segment.Start, segment.End, segment.Hours, segment.Multiplier, segment.RatePerHour, segment.SegmentCost);
}
