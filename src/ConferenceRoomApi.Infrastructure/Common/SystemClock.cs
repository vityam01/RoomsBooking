using ConferenceRoomApi.Application.Common.Interfaces;
using ConferenceRoomApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ConferenceRoomApi.Infrastructure.Common;

/// <summary>Real-clock <see cref="IClock"/> implementation, resolving "today" in the venue's own time zone rather than UTC.</summary>
public sealed class SystemClock : IClock
{
    private readonly TimeZoneInfo _businessTimeZone;

    public SystemClock(IOptions<BusinessSettings> settings)
    {
        _businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.TimeZoneId);
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _businessTimeZone).Date);
}
