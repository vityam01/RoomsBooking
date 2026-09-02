namespace ConferenceRoomApi.Domain.Pricing;

/// <summary>The window during which the venue accepts bookings at all.</summary>
public static class BusinessHours
{
    public static readonly TimeOnly OpensAt = new(6, 0);
    public static readonly TimeOnly ClosesAt = new(23, 0);
}
