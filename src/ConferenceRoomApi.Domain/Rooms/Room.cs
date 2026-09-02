namespace ConferenceRoomApi.Domain.Rooms;

/// <summary>
/// A conference room available for rent. Encapsulates its own invariants (capacity,
/// pricing, which additional services it offers) so those rules cannot be bypassed by
/// callers poking at properties directly.
/// </summary>
public sealed class Room
{
    private readonly List<RoomOffering> _offerings = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public int Capacity { get; private set; }
    public decimal BasePricePerHour { get; private set; }

    /// <summary>
    /// Soft-delete flag. Rooms are never hard-deleted: past bookings must keep referring
    /// to a real room row so historical revenue and reports stay accurate.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<RoomOffering> Offerings => _offerings.AsReadOnly();

    private Room()
    {
        // Required by EF Core.
    }

    public static Room Create(string name, int capacity, decimal basePricePerHour)
    {
        var room = new Room();
        room.Id = Guid.NewGuid();
        room.SetName(name);
        room.SetCapacity(capacity);
        room.SetBasePricePerHour(basePricePerHour);
        room.IsActive = true;
        room.CreatedAt = DateTimeOffset.UtcNow;
        room.UpdatedAt = room.CreatedAt;
        return room;
    }

    public void UpdateDetails(string name, int capacity, decimal basePricePerHour)
    {
        SetName(name);
        SetCapacity(capacity);
        SetBasePricePerHour(basePricePerHour);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Replaces the set of additional services this room offers with the given catalog ids.
    /// Callers are expected to pass ids that already exist and are active; this method only
    /// enforces the room-side invariant (no duplicate offerings).
    /// </summary>
    public void ReplaceOfferings(IEnumerable<Guid> additionalServiceIds)
    {
        _offerings.Clear();
        foreach (var serviceId in additionalServiceIds.Distinct())
        {
            _offerings.Add(new RoomOffering(Id, serviceId));
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Room name must not be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetCapacity(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Room capacity must be a positive number.", nameof(capacity));
        }

        Capacity = capacity;
    }

    private void SetBasePricePerHour(decimal basePricePerHour)
    {
        if (basePricePerHour <= 0)
        {
            throw new ArgumentException("Base price per hour must be a positive number.", nameof(basePricePerHour));
        }

        BasePricePerHour = basePricePerHour;
    }
}
