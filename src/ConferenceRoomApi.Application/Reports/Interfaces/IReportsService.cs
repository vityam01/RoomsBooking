using ConferenceRoomApi.Application.Reports.Dtos;

namespace ConferenceRoomApi.Application.Reports.Interfaces;

/// <summary>
/// Business-facing analytics over booking history. Implemented directly against the
/// persistence store (see Infrastructure) rather than through the write-side repositories,
/// since these are bespoke read/aggregation queries that don't belong on Room or Booking's
/// transactional repository interfaces.
/// </summary>
public interface IReportsService
{
    Task<RevenueReportDto> GetRevenueAsync(DateOnly from, DateOnly to, RevenueGroupBy groupBy, CancellationToken cancellationToken = default);

    Task<RoomUtilizationReportDto> GetRoomUtilizationAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<PopularServicesReportDto> GetPopularServicesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
