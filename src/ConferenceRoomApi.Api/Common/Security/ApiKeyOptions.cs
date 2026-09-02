namespace ConferenceRoomApi.Api.Common.Security;

/// <summary>Bound from the "Security" configuration section.</summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Accepted values for the X-Api-Key header on state-changing requests. Left empty by
    /// default (e.g. local development) the check is skipped entirely — set at least one
    /// key in production to turn it on. This is a pragmatic service-to-service guard, not a
    /// substitute for real user identity: a system growing past internal/trusted clients
    /// should replace it with OAuth2/JWT via an identity provider.
    /// </summary>
    public HashSet<string> ApiKeys { get; set; } = new();
}
