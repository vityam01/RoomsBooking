using ConferenceRoomApi.Application.AdditionalServices.Dtos;

namespace ConferenceRoomApi.Application.Rooms.Dtos;

public sealed record RoomDto(
    Guid Id,
    string Name,
    int Capacity,
    decimal BasePricePerHour,
    bool IsActive,
    IReadOnlyCollection<AdditionalServiceDto> Services,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateRoomRequest(string Name, int Capacity, decimal BasePricePerHour, List<Guid>? AdditionalServiceIds);

public sealed record UpdateRoomRequest(string Name, int Capacity, decimal BasePricePerHour, List<Guid>? AdditionalServiceIds);
