using ConferenceRoomApi.Application.AdditionalServices.Dtos;
using ConferenceRoomApi.Application.AdditionalServices.Interfaces;
using ConferenceRoomApi.Application.Bookings.Interfaces;
using ConferenceRoomApi.Application.Common.Interfaces;
using ConferenceRoomApi.Application.Pricing;
using ConferenceRoomApi.Application.Rooms.Dtos;
using ConferenceRoomApi.Application.Rooms.Interfaces;
using ConferenceRoomApi.Domain.AdditionalServices;
using ConferenceRoomApi.Domain.Common.Exceptions;
using ConferenceRoomApi.Domain.Pricing;
using ConferenceRoomApi.Domain.Rooms;

namespace ConferenceRoomApi.Application.Rooms;

/// <summary>Use cases for managing conference rooms and searching for availability.</summary>
public sealed class RoomsService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IAdditionalServiceRepository _serviceRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public RoomsService(
        IRoomRepository roomRepository,
        IAdditionalServiceRepository serviceRepository,
        IBookingRepository bookingRepository,
        IPricingPolicy pricingPolicy,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository;
        _serviceRepository = serviceRepository;
        _bookingRepository = bookingRepository;
        _pricingPolicy = pricingPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<RoomDto>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var rooms = await _roomRepository.ListAsync(includeInactive, cancellationToken);
        return rooms.Select(ToDto).ToList();
    }

    public async Task<RoomDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var room = await _roomRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Room), id);
        return ToDto(room);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var offeredServices = await ResolveOfferedServicesAsync(request.AdditionalServiceIds, cancellationToken);

        var room = Room.Create(request.Name, request.Capacity, request.BasePricePerHour);
        room.ReplaceOfferings(offeredServices.Select(s => s.Id));

        _roomRepository.Add(room);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(room, offeredServices);
    }

    public async Task<RoomDto> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var room = await _roomRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Room), id);

        var offeredServices = await ResolveOfferedServicesAsync(request.AdditionalServiceIds, cancellationToken);

        room.UpdateDetails(request.Name, request.Capacity, request.BasePricePerHour);
        room.ReplaceOfferings(offeredServices.Select(s => s.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(room, offeredServices);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var room = await _roomRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Room), id);

        // Soft delete: past bookings must keep pointing at a real room row for reporting.
        room.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AvailableRoomDto>> SearchAvailableAsync(SearchAvailableRoomsRequest request, CancellationToken cancellationToken = default)
    {
        var candidates = await _roomRepository.ListActiveByMinCapacityAsync(request.Capacity, cancellationToken);
        if (candidates.Count == 0)
        {
            return new List<AvailableRoomDto>();
        }

        var bookedRoomIds = await _bookingRepository.GetBookedRoomIdsAsync(
            candidates.Select(r => r.Id), request.Date, request.StartTime, request.EndTime, cancellationToken);

        return candidates
            .Where(r => !bookedRoomIds.Contains(r.Id))
            .Select(room =>
            {
                var breakdown = _pricingPolicy.Calculate(room.BasePricePerHour, request.StartTime, request.EndTime);
                var services = room.Offerings.Select(o => o.AdditionalService).Where(s => s.IsActive).Select(AdditionalServiceDto.FromDomain).ToList();

                return new AvailableRoomDto(
                    room.Id,
                    room.Name,
                    room.Capacity,
                    room.BasePricePerHour,
                    services,
                    breakdown.TotalCost,
                    breakdown.Segments.Select(PriceSegmentDto.FromDomain).ToList());
            })
            .OrderBy(r => r.EstimatedRoomCost)
            .ToList();
    }

    private async Task<List<AdditionalService>> ResolveOfferedServicesAsync(List<Guid>? requestedIds, CancellationToken cancellationToken)
    {
        if (requestedIds is null || requestedIds.Count == 0)
        {
            return new List<AdditionalService>();
        }

        var distinctIds = requestedIds.Distinct().ToList();
        var resolved = await _serviceRepository.GetActiveByIdsAsync(distinctIds, cancellationToken);

        if (resolved.Count != distinctIds.Count)
        {
            var missing = distinctIds.Except(resolved.Select(s => s.Id));
            throw new BusinessRuleViolationException(
                $"The following additional service ids are unknown or inactive: {string.Join(", ", missing)}.");
        }

        return resolved;
    }

    private static RoomDto ToDto(Room room)
        => ToDto(room, room.Offerings.Select(o => o.AdditionalService).Where(s => s.IsActive).ToList());

    private static RoomDto ToDto(Room room, IReadOnlyCollection<AdditionalService> services)
        => new(
            room.Id,
            room.Name,
            room.Capacity,
            room.BasePricePerHour,
            room.IsActive,
            services.Select(AdditionalServiceDto.FromDomain).ToList(),
            room.CreatedAt,
            room.UpdatedAt);
}
