namespace ConferenceRoomApi.Application.Common.Interfaces;

/// <summary>
/// Abstraction over "now", injected everywhere the application layer needs the current time
/// (e.g. rejecting bookings in the past). Keeps use cases deterministic and testable instead
/// of calling <see cref="DateTimeOffset.UtcNow"/> directly.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}
