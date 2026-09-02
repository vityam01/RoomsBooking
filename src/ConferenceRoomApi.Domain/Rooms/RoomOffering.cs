using ConferenceRoomApi.Domain.AdditionalServices;

namespace ConferenceRoomApi.Domain.Rooms;

/// <summary>
/// Join entity linking a <see cref="Room"/> to an <see cref="AdditionalService"/> it offers.
/// A dedicated entity (rather than a bare many-to-many) leaves room to attach
/// room-specific attributes later (e.g. a per-room price override) without a migration
/// that changes the shape of the relationship itself.
/// </summary>
public sealed class RoomOffering
{
    public Guid RoomId { get; private set; }
    public Guid AdditionalServiceId { get; private set; }
    public AdditionalService AdditionalService { get; private set; } = default!;

    private RoomOffering()
    {
        // Required by EF Core.
    }

    internal RoomOffering(Guid roomId, Guid additionalServiceId)
    {
        RoomId = roomId;
        AdditionalServiceId = additionalServiceId;
    }
}
