using ConferenceRoomApi.Application.Rooms.Interfaces;
using ConferenceRoomApi.Domain.Rooms;
using ConferenceRoomApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomApi.Infrastructure.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _db;

    public RoomRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Rooms
            .Include(r => r.Offerings).ThenInclude(o => o.AdditionalService)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<List<Room>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _db.Rooms.Include(r => r.Offerings).ThenInclude(o => o.AdditionalService).AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        return query.OrderBy(r => r.Name).ToListAsync(cancellationToken);
    }

    public Task<List<Room>> ListActiveByMinCapacityAsync(int minCapacity, CancellationToken cancellationToken = default)
        => _db.Rooms
            .Include(r => r.Offerings).ThenInclude(o => o.AdditionalService)
            .Where(r => r.IsActive && r.Capacity >= minCapacity)
            .OrderBy(r => r.BasePricePerHour)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _db.Rooms
            .Where(r => idList.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);
    }

    public void Add(Room room) => _db.Rooms.Add(room);
}
