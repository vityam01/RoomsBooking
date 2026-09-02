using ConferenceRoomApi.Domain.Common.Exceptions;
using ConferenceRoomApi.Domain.Pricing;

namespace ConferenceRoomApi.Domain.Bookings;

/// <summary>
/// A confirmed (or later cancelled) reservation of a room for a time window, with a frozen
/// cost breakdown. All monetary figures on a Booking are snapshots taken at creation time —
/// they never recompute from the room's current price, so revenue reports stay accurate
/// even after prices change.
/// </summary>
public sealed class Booking
{
    private readonly List<BookedService> _bookedServices = new();
    private readonly List<BookedPriceSegment> _priceSegments = new();

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public DateOnly BookingDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public BookingStatus Status { get; private set; }
    public decimal RoomCost { get; private set; }
    public decimal ServicesCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public IReadOnlyCollection<BookedService> BookedServices => _bookedServices.AsReadOnly();
    public IReadOnlyCollection<BookedPriceSegment> PriceSegments => _priceSegments.AsReadOnly();

    private Booking()
    {
        // Required by EF Core.
    }

    public static Booking Create(
        Guid roomId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly today,
        RoomCostBreakdown roomCostBreakdown,
        IReadOnlyCollection<SelectedServiceSnapshot> selectedServices)
    {
        if (bookingDate < today)
        {
            throw new BusinessRuleViolationException("Cannot book a room for a date in the past.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTimeOffset.UtcNow
        };

        booking._priceSegments.AddRange(roomCostBreakdown.Segments.Select(s => new BookedPriceSegment(s)));
        booking.RoomCost = roomCostBreakdown.TotalCost;

        foreach (var service in selectedServices)
        {
            booking._bookedServices.Add(new BookedService(service.AdditionalServiceId, service.Name, service.Price));
        }

        booking.ServicesCost = booking._bookedServices.Sum(s => s.Price);
        booking.TotalCost = booking.RoomCost + booking.ServicesCost;

        return booking;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new BusinessRuleViolationException("Booking is already cancelled.");
        }

        Status = BookingStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public bool OverlapsWith(TimeOnly otherStart, TimeOnly otherEnd)
        => Status == BookingStatus.Confirmed && TimeRangeOverlap.Overlaps(StartTime, EndTime, otherStart, otherEnd);
}
