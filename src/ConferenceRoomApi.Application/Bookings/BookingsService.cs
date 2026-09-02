using ConferenceRoomApi.Application.Bookings.Dtos;
using ConferenceRoomApi.Application.Bookings.Interfaces;
using ConferenceRoomApi.Application.Common.Dtos;
using ConferenceRoomApi.Application.Common.Interfaces;
using ConferenceRoomApi.Application.Pricing;
using ConferenceRoomApi.Application.Rooms.Interfaces;
using ConferenceRoomApi.Domain.AdditionalServices;
using ConferenceRoomApi.Domain.Bookings;
using ConferenceRoomApi.Domain.Common.Exceptions;
using ConferenceRoomApi.Domain.Pricing;
using ConferenceRoomApi.Domain.Rooms;

namespace ConferenceRoomApi.Application.Bookings;

/// <summary>
/// Use cases for booking rooms. <see cref="CreateAsync"/> is the heart of the whole system:
/// it validates the room and services, prices the slot, and guards against double-booking
/// with an in-transaction overlap check (the database's EXCLUDE constraint is the final
/// backstop for the race a concurrent request could still slip through).
/// </summary>
public sealed class BookingsService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IPricingPolicy _pricingPolicy;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public BookingsService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IPricingPolicy pricingPolicy,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _pricingPolicy = pricingPolicy;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var room = await _roomRepository.GetByIdAsync(request.RoomId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Room), request.RoomId);

        if (!room.IsActive)
        {
            throw new BusinessRuleViolationException("This room has been removed and can no longer be booked.");
        }

        var selectedServices = ResolveSelectedServices(room, request.AdditionalServiceIds);
        var breakdown = _pricingPolicy.Calculate(room.BasePricePerHour, request.StartTime, request.EndTime);

        await EnsureNoOverlapAsync(room.Id, request.Date, request.StartTime, request.EndTime, cancellationToken);

        var booking = Booking.Create(
            room.Id,
            request.Date,
            request.StartTime,
            request.EndTime,
            _clock.Today,
            breakdown,
            selectedServices);

        _bookingRepository.Add(booking);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(booking, room.Name);
    }

    public async Task<BookingDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Booking), id);
        var room = await _roomRepository.GetByIdAsync(booking.RoomId, cancellationToken);
        return ToDto(booking, room?.Name ?? "(deleted room)");
    }

    public async Task<PagedResult<BookingDto>> ListAsync(BookingListFilter filter, CancellationToken cancellationToken = default)
    {
        var (bookings, totalCount) = await _bookingRepository.ListAsync(filter, cancellationToken);
        var roomNames = await _roomRepository.GetNamesByIdsAsync(bookings.Select(b => b.RoomId), cancellationToken);
        var items = bookings.Select(b => ToDto(b, roomNames.GetValueOrDefault(b.RoomId, "(deleted room)"))).ToList();
        return new PagedResult<BookingDto>(items, filter.Page, filter.PageSize, totalCount);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Booking), id);

        booking.Cancel();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static List<SelectedServiceSnapshot> ResolveSelectedServices(Room room, List<Guid>? requestedIds)
    {
        if (requestedIds is null || requestedIds.Count == 0)
        {
            return new List<SelectedServiceSnapshot>();
        }

        // Only active services are bookable — a service can be deactivated after a room
        // already offers it, and the RoomOffering row is deliberately left in place (it's
        // history, not a live "still bookable" signal).
        var offeredById = room.Offerings
            .Where(o => o.AdditionalService.IsActive)
            .ToDictionary(o => o.AdditionalServiceId, o => o.AdditionalService);
        var distinctIds = requestedIds.Distinct().ToList();
        var notOffered = distinctIds.Where(id => !offeredById.ContainsKey(id)).ToList();

        if (notOffered.Count > 0)
        {
            throw new BusinessRuleViolationException(
                $"Room '{room.Id}' does not offer the following services: {string.Join(", ", notOffered)}.");
        }

        return distinctIds
            .Select(id => offeredById[id])
            .Select(service => new SelectedServiceSnapshot(service.Id, service.Name, service.Price))
            .ToList();
    }

    private async Task EnsureNoOverlapAsync(Guid roomId, DateOnly date, TimeOnly start, TimeOnly end, CancellationToken cancellationToken)
    {
        var sameDayBookings = await _bookingRepository.ListConfirmedForRoomOnDateAsync(roomId, date, cancellationToken);
        if (sameDayBookings.Any(b => b.OverlapsWith(start, end)))
        {
            throw new RoomUnavailableException(roomId, date, start, end);
        }
    }

    private static BookingDto ToDto(Booking booking, string roomName)
        => new(
            booking.Id,
            booking.RoomId,
            roomName,
            booking.BookingDate,
            booking.StartTime,
            booking.EndTime,
            booking.Status.ToString(),
            booking.RoomCost,
            booking.ServicesCost,
            booking.TotalCost,
            booking.PriceSegments
                .OrderBy(s => s.Start)
                .Select(s => new PriceSegmentDto(s.Zone, s.Start, s.End, s.Hours, s.Multiplier, s.RatePerHour, s.SegmentCost))
                .ToList(),
            booking.BookedServices.Select(s => new BookedServiceDto(s.AdditionalServiceId, s.Name, s.Price)).ToList(),
            booking.CreatedAt,
            booking.CancelledAt);
}
