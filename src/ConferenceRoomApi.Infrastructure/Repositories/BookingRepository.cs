using ConferenceRoomApi.Application.Bookings.Dtos;
using ConferenceRoomApi.Application.Bookings.Interfaces;
using ConferenceRoomApi.Domain.Bookings;
using ConferenceRoomApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomApi.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;

    public BookingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Bookings
            .Include(b => b.BookedServices)
            .Include(b => b.PriceSegments)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<List<Booking>> ListConfirmedForRoomOnDateAsync(Guid roomId, DateOnly date, CancellationToken cancellationToken = default)
        => _db.Bookings
            .Where(b => b.RoomId == roomId && b.BookingDate == date && b.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);

    public async Task<HashSet<Guid>> GetBookedRoomIdsAsync(
        IEnumerable<Guid> roomIds, DateOnly date, TimeOnly start, TimeOnly end, CancellationToken cancellationToken = default)
    {
        var idList = roomIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new HashSet<Guid>();
        }

        // Mirrors ConferenceRoomApi.Domain.Bookings.TimeRangeOverlap.Overlaps: aStart < bEnd && bStart < aEnd.
        var bookedIds = await _db.Bookings
            .Where(b => idList.Contains(b.RoomId)
                        && b.BookingDate == date
                        && b.Status == BookingStatus.Confirmed
                        && b.StartTime < end
                        && start < b.EndTime)
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return bookedIds.ToHashSet();
    }

    public Task<List<Booking>> ListAsync(BookingListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.Bookings
            .Include(b => b.BookedServices)
            .Include(b => b.PriceSegments)
            .AsQueryable();

        if (filter.RoomId is { } roomId)
        {
            query = query.Where(b => b.RoomId == roomId);
        }

        if (filter.From is { } from)
        {
            query = query.Where(b => b.BookingDate >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(b => b.BookingDate <= to);
        }

        if (!filter.IncludeCancelled)
        {
            query = query.Where(b => b.Status == BookingStatus.Confirmed);
        }

        return query.OrderByDescending(b => b.BookingDate).ThenBy(b => b.StartTime).ToListAsync(cancellationToken);
    }

    public void Add(Booking booking) => _db.Bookings.Add(booking);
}
