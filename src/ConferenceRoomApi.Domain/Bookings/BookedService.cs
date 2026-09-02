namespace ConferenceRoomApi.Domain.Bookings;

/// <summary>
/// A snapshot of one additional service selected for a booking. Name and price are copied
/// at booking time (not referenced live from the catalog) so that editing or deactivating a
/// catalog service later never changes the price of a past booking.
/// </summary>
public sealed class BookedService
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid AdditionalServiceId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }

    private BookedService()
    {
        // Required by EF Core.
    }

    internal BookedService(Guid additionalServiceId, string name, decimal price)
    {
        Id = Guid.NewGuid();
        AdditionalServiceId = additionalServiceId;
        Name = name;
        Price = price;
    }
}
