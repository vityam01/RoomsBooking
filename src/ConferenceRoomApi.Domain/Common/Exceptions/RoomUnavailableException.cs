namespace ConferenceRoomApi.Domain.Common.Exceptions;

/// <summary>
/// Thrown when a booking is attempted for a room/time slot that is already taken.
/// Maps to HTTP 409 Conflict — distinct from 400 because the request itself is valid,
/// it just lost a race against concurrent state.
/// </summary>
public sealed class RoomUnavailableException : DomainException
{
    public RoomUnavailableException(Guid roomId, DateOnly date, TimeOnly start, TimeOnly end)
        : base($"Room '{roomId}' is already booked on {date:yyyy-MM-dd} between {start:HH:mm} and {end:HH:mm}.")
    {
    }
}
