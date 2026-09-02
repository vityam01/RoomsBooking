namespace ConferenceRoomApi.Domain.Pricing;

/// <summary>
/// The cost of renting a room for one uninterrupted sub-interval of a booking that falls
/// entirely within a single rate zone. A booking that spans multiple zones (e.g. 11:00–15:00,
/// which crosses Standard → Peak → Standard) is priced as one segment per zone crossed.
/// </summary>
public sealed record PriceSegment(
    RateZoneType Zone,
    TimeOnly Start,
    TimeOnly End,
    decimal Hours,
    decimal Multiplier,
    decimal RatePerHour,
    decimal SegmentCost);
