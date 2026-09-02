using ConferenceRoomApi.Application.Reports.Dtos;
using ConferenceRoomApi.Application.Reports.Interfaces;
using ConferenceRoomApi.Domain.Bookings;
using ConferenceRoomApi.Domain.Pricing;
using ConferenceRoomApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomApi.Infrastructure.Reports;

/// <summary>
/// Aggregation queries over booking history. Deliberately bypasses the write-side
/// repositories and reads straight off the DbContext — reports are read-only, ad hoc
/// shapes that don't belong on Room/Booking's transactional interfaces, and at the data
/// volumes this system deals with, pulling the period's bookings into memory once and
/// aggregating with LINQ is simpler and just as fast as pushing every grouping into SQL.
/// </summary>
public sealed class ReportsService : IReportsService
{
    private readonly ApplicationDbContext _db;

    public ReportsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RevenueReportDto> GetRevenueAsync(
        DateOnly from, DateOnly to, RevenueGroupBy groupBy, CancellationToken cancellationToken = default)
    {
        var bookings = await ConfirmedBookingsInRange(from, to).ToListAsync(cancellationToken);

        List<RevenueBucketDto> buckets;
        if (groupBy == RevenueGroupBy.Day)
        {
            buckets = bookings
                .GroupBy(b => b.BookingDate)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueBucketDto(
                    g.Key.ToString("yyyy-MM-dd"), g.Count(), g.Sum(b => b.RoomCost), g.Sum(b => b.ServicesCost), g.Sum(b => b.TotalCost)))
                .ToList();
        }
        else
        {
            var roomNames = await _db.Rooms
                .Where(r => bookings.Select(b => b.RoomId).Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

            buckets = bookings
                .GroupBy(b => b.RoomId)
                .Select(g => new RevenueBucketDto(
                    roomNames.GetValueOrDefault(g.Key, "(deleted room)"),
                    g.Count(), g.Sum(b => b.RoomCost), g.Sum(b => b.ServicesCost), g.Sum(b => b.TotalCost)))
                .OrderByDescending(b => b.TotalRevenue)
                .ToList();
        }

        return new RevenueReportDto(from, to, bookings.Sum(b => b.TotalCost), buckets);
    }

    public async Task<RoomUtilizationReportDto> GetRoomUtilizationAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var rooms = await _db.Rooms.Where(r => r.IsActive).ToListAsync(cancellationToken);
        var bookings = await ConfirmedBookingsInRange(from, to).ToListAsync(cancellationToken);

        var totalDays = to.DayNumber - from.DayNumber + 1;
        var dailyOperatingHours = (decimal)(BusinessHours.ClosesAt - BusinessHours.OpensAt).TotalHours;
        var availableHours = totalDays * dailyOperatingHours;

        var rows = rooms
            .Select(room =>
            {
                var roomBookings = bookings.Where(b => b.RoomId == room.Id).ToList();
                var bookedHours = roomBookings.Sum(b => (decimal)(b.EndTime - b.StartTime).Ticks / TimeSpan.TicksPerHour);
                var occupancy = availableHours > 0 ? bookedHours / availableHours : 0m;

                return new RoomUtilizationDto(room.Id, room.Name, roomBookings.Count, bookedHours, availableHours, Math.Round(occupancy, 4));
            })
            .OrderByDescending(r => r.OccupancyRate)
            .ToList();

        return new RoomUtilizationReportDto(from, to, rows);
    }

    public async Task<PopularServicesReportDto> GetPopularServicesAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var bookingIds = await ConfirmedBookingsInRange(from, to).Select(b => b.Id).ToListAsync(cancellationToken);

        var services = await _db.Set<BookedService>()
            .Where(s => bookingIds.Contains(s.BookingId))
            .GroupBy(s => new { s.AdditionalServiceId, s.Name })
            .Select(g => new PopularServiceDto(g.Key.AdditionalServiceId, g.Key.Name, g.Count(), g.Sum(s => s.Price)))
            .OrderByDescending(s => s.TimesBooked)
            .ToListAsync(cancellationToken);

        return new PopularServicesReportDto(from, to, services);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var bookings = await _db.Bookings
            .Include(b => b.PriceSegments)
            .Where(b => b.BookingDate >= from && b.BookingDate <= to)
            .ToListAsync(cancellationToken);

        var confirmed = bookings.Where(b => b.Status == BookingStatus.Confirmed).ToList();
        var cancelledCount = bookings.Count(b => b.Status == BookingStatus.Cancelled);
        var totalRevenue = confirmed.Sum(b => b.TotalCost);
        var averageBookingValue = confirmed.Count > 0 ? totalRevenue / confirmed.Count : 0m;

        var mostBookedRoomId = confirmed
            .GroupBy(b => b.RoomId)
            .OrderByDescending(g => g.Count())
            .Select(g => (Guid?)g.Key)
            .FirstOrDefault();

        var mostBookedRoomName = mostBookedRoomId is null
            ? null
            : await _db.Rooms.Where(r => r.Id == mostBookedRoomId).Select(r => r.Name).FirstOrDefaultAsync(cancellationToken);

        var rateZoneUsage = confirmed
            .SelectMany(b => b.PriceSegments)
            .GroupBy(s => s.Zone)
            .Select(g => new RateZoneUsageDto(g.Key, g.Count(), g.Sum(s => s.Hours), g.Sum(s => s.SegmentCost)))
            .OrderByDescending(z => z.Revenue)
            .ToList();

        return new DashboardSummaryDto(from, to, confirmed.Count, cancelledCount, totalRevenue, averageBookingValue, mostBookedRoomName, rateZoneUsage);
    }

    private IQueryable<Booking> ConfirmedBookingsInRange(DateOnly from, DateOnly to)
        => _db.Bookings.Where(b => b.BookingDate >= from && b.BookingDate <= to && b.Status == BookingStatus.Confirmed);
}
