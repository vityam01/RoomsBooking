using ConferenceRoomApi.Application.AdditionalServices.Dtos;
using ConferenceRoomApi.Application.AdditionalServices.Interfaces;
using ConferenceRoomApi.Application.Common.Interfaces;
using ConferenceRoomApi.Domain.AdditionalServices;
using ConferenceRoomApi.Domain.Common.Exceptions;

namespace ConferenceRoomApi.Application.AdditionalServices;

/// <summary>Use cases for managing the global catalog of additional services (projector, Wi-Fi, ...).</summary>
public sealed class AdditionalServicesService
{
    private readonly IAdditionalServiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AdditionalServicesService(IAdditionalServiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AdditionalServiceDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var services = await _repository.ListActiveAsync(cancellationToken);
        return services.Select(AdditionalServiceDto.FromDomain).ToList();
    }

    public async Task<AdditionalServiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(AdditionalService), id);
        return AdditionalServiceDto.FromDomain(service);
    }

    public async Task<AdditionalServiceDto> CreateAsync(CreateAdditionalServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = AdditionalService.Create(request.Name, request.Price);
        _repository.Add(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AdditionalServiceDto.FromDomain(service);
    }

    public async Task<AdditionalServiceDto> UpdateAsync(Guid id, UpdateAdditionalServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(AdditionalService), id);

        service.Update(request.Name, request.Price);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AdditionalServiceDto.FromDomain(service);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(AdditionalService), id);

        service.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
