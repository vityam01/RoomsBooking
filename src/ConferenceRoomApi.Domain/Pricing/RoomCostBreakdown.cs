namespace ConferenceRoomApi.Domain.Pricing;

/// <summary>The itemized, auditable result of pricing a room rental for a time range.</summary>
public sealed record RoomCostBreakdown(IReadOnlyList<PriceSegment> Segments, decimal TotalCost);
