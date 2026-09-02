using ConferenceRoomApi.Domain.AdditionalServices;

namespace ConferenceRoomApi.Application.AdditionalServices.Interfaces;

public interface IAdditionalServiceRepository
{
    Task<AdditionalService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns only the active services among the given ids (missing/inactive ids are silently dropped).</summary>
    Task<List<AdditionalService>> GetActiveByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<List<AdditionalService>> ListActiveAsync(CancellationToken cancellationToken = default);

    void Add(AdditionalService service);
}
