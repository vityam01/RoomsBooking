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

    public async Task<(List<Booking> Items, int TotalCount, int Page, int PageSize)> ListAsync(
        BookingListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.Bookings.AsQueryable();

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

        // Count against the un-Included query — cheaper, and Include would be dropped from
        // the generated SQL anyway, but this keeps the intent explicit.
        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(b => b.BookingDate).ThenBy(b => b.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.BookedServices)
            .Include(b => b.PriceSegments)
            .ToListAsync(cancellationToken);

        return (items, totalCount, page, pageSize);
    }

    public void Add(Booking booking) => _db.Bookings.Add(booking);
}
