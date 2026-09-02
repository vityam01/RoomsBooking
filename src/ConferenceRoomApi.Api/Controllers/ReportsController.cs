using ConferenceRoomApi.Application.Reports.Dtos;
using ConferenceRoomApi.Application.Reports.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomApi.Api.Controllers;

/// <summary>Business analytics over booking history: revenue, room utilization, and service popularity.</summary>
[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    /// <summary>Revenue for a date range, grouped by day or by room.</summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] RevenueReportRequest request, CancellationToken cancellationToken)
        => Ok(await _reportsService.GetRevenueAsync(request.From, request.To, request.GroupBy, cancellationToken));

    /// <summary>Booked hours vs. available hours per room, for spotting under- and over-used rooms.</summary>
    [HttpGet("room-utilization")]
    [ProducesResponseType(typeof(RoomUtilizationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoomUtilizationReportDto>> RoomUtilization([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
        => Ok(await _reportsService.GetRoomUtilizationAsync(request.From, request.To, cancellationToken));

    /// <summary>How often each additional service was booked, and the revenue it generated.</summary>
    [HttpGet("popular-services")]
    [ProducesResponseType(typeof(PopularServicesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PopularServicesReportDto>> PopularServices([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
        => Ok(await _reportsService.GetPopularServicesAsync(request.From, request.To, cancellationToken));

    /// <summary>A dashboard-style rollup: booking counts, revenue, and rate-zone usage for a date range.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
        => Ok(await _reportsService.GetDashboardSummaryAsync(request.From, request.To, cancellationToken));
}
