using ConferenceRoomApi.Application.AdditionalServices.Interfaces;
using ConferenceRoomApi.Domain.AdditionalServices;
using ConferenceRoomApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomApi.Infrastructure.Repositories;

public sealed class AdditionalServiceRepository : IAdditionalServiceRepository
{
    private readonly ApplicationDbContext _db;

    public AdditionalServiceRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<AdditionalService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.AdditionalServices.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<List<AdditionalService>> GetActiveByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return _db.AdditionalServices
            .Where(s => idList.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task<List<AdditionalService>> ListActiveAsync(CancellationToken cancellationToken = default)
        => _db.AdditionalServices.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public void Add(AdditionalService service) => _db.AdditionalServices.Add(service);
}
