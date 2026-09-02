using ConferenceRoomApi.Domain.AdditionalServices;

namespace ConferenceRoomApi.Application.AdditionalServices.Dtos;

public sealed record AdditionalServiceDto(Guid Id, string Name, decimal Price, bool IsActive)
{
    public static AdditionalServiceDto FromDomain(AdditionalService service)
        => new(service.Id, service.Name, service.Price, service.IsActive);
}

public sealed record CreateAdditionalServiceRequest(string Name, decimal Price);

public sealed record UpdateAdditionalServiceRequest(string Name, decimal Price);
