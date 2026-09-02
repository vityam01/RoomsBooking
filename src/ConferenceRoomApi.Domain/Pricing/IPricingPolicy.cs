namespace ConferenceRoomApi.Domain.Pricing;

/// <summary>
/// Computes the room-rental portion of a booking's cost. Kept as an interface so the rate
/// table can be swapped or made tenant-specific later (open/closed principle) without
/// touching the booking workflow that consumes it.
/// </summary>
public interface IPricingPolicy
{
    RoomCostBreakdown Calculate(decimal basePricePerHour, TimeOnly startTime, TimeOnly endTime);
}
