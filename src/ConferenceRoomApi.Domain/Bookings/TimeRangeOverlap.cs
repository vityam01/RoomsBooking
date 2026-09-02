namespace ConferenceRoomApi.Domain.Bookings;

/// <summary>
/// Half-open interval overlap check ([start, end)), shared by availability search and the
/// booking conflict check so both use exactly the same definition of "overlaps" — a booking
/// ending at 12:00 does not conflict with one starting at 12:00.
/// </summary>
public static class TimeRangeOverlap
{
    public static bool Overlaps(TimeOnly aStart, TimeOnly aEnd, TimeOnly bStart, TimeOnly bEnd)
        => aStart < bEnd && bStart < aEnd;
}
