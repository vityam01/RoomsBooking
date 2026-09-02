using ConferenceRoomApi.Domain.Pricing;

namespace ConferenceRoomApi.Application.Reports.Dtos;

public enum RevenueGroupBy
{
    Day,
    Room
}

public sealed record RevenueBucketDto(string Key, int BookingCount, decimal RoomRevenue, decimal ServicesRevenue, decimal TotalRevenue);

public sealed record RevenueReportDto(DateOnly From, DateOnly To, decimal TotalRevenue, IReadOnlyCollection<RevenueBucketDto> Buckets);

public sealed record RoomUtilizationDto(
    Guid RoomId, string RoomName, int BookingCount, decimal BookedHours, decimal AvailableHours, decimal OccupancyRate);

public sealed record RoomUtilizationReportDto(DateOnly From, DateOnly To, IReadOnlyCollection<RoomUtilizationDto> Rooms);

public sealed record PopularServiceDto(Guid AdditionalServiceId, string Name, int TimesBooked, decimal Revenue);

public sealed record PopularServicesReportDto(DateOnly From, DateOnly To, IReadOnlyCollection<PopularServiceDto> Services);

public sealed record RateZoneUsageDto(RateZoneType Zone, int SegmentCount, decimal Hours, decimal Revenue);

public sealed record DashboardSummaryDto(
    DateOnly From,
    DateOnly To,
    int ConfirmedBookings,
    int CancelledBookings,
    decimal TotalRevenue,
    decimal AverageBookingValue,
    string? MostBookedRoomName,
    IReadOnlyCollection<RateZoneUsageDto> RateZoneUsage);
