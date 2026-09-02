using ConferenceRoomApi.Application.Bookings.Dtos;
using ConferenceRoomApi.Domain.Bookings; // referenced by <see cref="TimeRangeOverlap"/> below

namespace ConferenceRoomApi.Application.Bookings.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>All confirmed bookings for one room on one date — small enough to check overlap in memory via <see cref="Booking.OverlapsWith"/>.</summary>
    Task<List<Booking>> ListConfirmedForRoomOnDateAsync(Guid roomId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids, among <paramref name="roomIds"/>, that already have a confirmed booking overlapping
    /// [start, end) on <paramref name="date"/>. Used by availability search across many rooms
    /// at once; the overlap predicate here MUST stay consistent with <see cref="TimeRangeOverlap.Overlaps"/>.
    /// </summary>
    Task<HashSet<Guid>> GetBookedRoomIdsAsync(
        IEnumerable<Guid> roomIds, DateOnly date, TimeOnly start, TimeOnly end, CancellationToken cancellationToken = default);

    /// <summary>Applies filter.Page/PageSize server-side (SQL OFFSET/LIMIT) — never loads a whole unbounded table into memory.</summary>
    Task<(List<Booking> Items, int TotalCount)> ListAsync(BookingListFilter filter, CancellationToken cancellationToken = default);

    void Add(Booking booking);
}
