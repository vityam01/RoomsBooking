namespace ConferenceRoomApi.Infrastructure.Options;

/// <summary>Business-wide configuration bound from the "Business" section of appsettings.</summary>
public sealed class BusinessSettings
{
    public const string SectionName = "Business";

    /// <summary>IANA time zone the venue operates in. "What day is it?" is answered relative to this, not UTC.</summary>
    public string TimeZoneId { get; set; } = "Europe/Kyiv";
}
