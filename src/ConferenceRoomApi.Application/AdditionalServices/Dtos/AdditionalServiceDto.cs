namespace ConferenceRoomApi.Application.AdditionalServices.Dtos;

public sealed record AdditionalServiceDto(Guid Id, string Name, decimal Price, bool IsActive);

public sealed record CreateAdditionalServiceRequest(string Name, decimal Price);

public sealed record UpdateAdditionalServiceRequest(string Name, decimal Price);
