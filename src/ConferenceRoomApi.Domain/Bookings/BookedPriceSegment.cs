using ConferenceRoomApi.Domain.Pricing;

namespace ConferenceRoomApi.Domain.Bookings;

/// <summary>
/// Persisted snapshot of one <see cref="PriceSegment"/> produced by the pricing policy at
/// booking time, so the cost breakdown shown to the client can always be reconstructed
/// later even if the room's base price or the rate table changes afterwards.
/// </summary>
public sealed class BookedPriceSegment
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public RateZoneType Zone { get; private set; }
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }
    public decimal Hours { get; private set; }
    public decimal Multiplier { get; private set; }
    public decimal RatePerHour { get; private set; }
    public decimal SegmentCost { get; private set; }

    private BookedPriceSegment()
    {
        // Required by EF Core.
    }

    internal BookedPriceSegment(PriceSegment segment)
    {
        Id = Guid.NewGuid();
        Zone = segment.Zone;
        Start = segment.Start;
        End = segment.End;
        Hours = segment.Hours;
        Multiplier = segment.Multiplier;
        RatePerHour = segment.RatePerHour;
        SegmentCost = segment.SegmentCost;
    }
}
