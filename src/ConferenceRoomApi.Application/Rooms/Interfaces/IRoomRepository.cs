using ConferenceRoomApi.Domain.Rooms;

namespace ConferenceRoomApi.Application.Rooms.Interfaces;

public interface IRoomRepository
{
    /// <summary>Loads a room (including its offered services) regardless of active status.</summary>
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Room>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<List<Room>> ListActiveByMinCapacityAsync(int minCapacity, CancellationToken cancellationToken = default);

    /// <summary>Batched name lookup for rendering lists (e.g. bookings) without one query per room.</summary>
    Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    void Add(Room room);
}
