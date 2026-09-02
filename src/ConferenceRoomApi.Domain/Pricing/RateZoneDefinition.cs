namespace ConferenceRoomApi.Domain.Pricing;

/// <summary>An immutable [Start, End) window of a single day with its price multiplier.</summary>
public sealed record RateZoneDefinition(RateZoneType Type, TimeOnly Start, TimeOnly End, decimal Multiplier);

/// <summary>
/// The business's rate table. Peak (12:00–14:00) is listed after Standard and carries a
/// higher priority so it wins on the overlap — see <see cref="StandardPricingPolicy"/>.
/// Changing prices or hours means changing exactly this list; nothing else in the pricing
/// engine encodes a magic hour number.
/// </summary>
public static class RateZoneCatalog
{
    public static readonly IReadOnlyList<RateZoneDefinition> Zones = new[]
    {
        new RateZoneDefinition(RateZoneType.Morning, new TimeOnly(6, 0), new TimeOnly(9, 0), 0.90m),
        new RateZoneDefinition(RateZoneType.Standard, new TimeOnly(9, 0), new TimeOnly(18, 0), 1.00m),
        new RateZoneDefinition(RateZoneType.Peak, new TimeOnly(12, 0), new TimeOnly(14, 0), 1.15m),
        new RateZoneDefinition(RateZoneType.Evening, new TimeOnly(18, 0), new TimeOnly(23, 0), 0.80m)
    };

    /// <summary>
    /// Zones ordered so that later entries take precedence when they overlap an earlier one
    /// at a given instant. Peak is a sub-window of Standard and must win.
    /// </summary>
    public static readonly IReadOnlyList<RateZoneType> PriorityOrder = new[]
    {
        RateZoneType.Morning,
        RateZoneType.Standard,
        RateZoneType.Evening,
        RateZoneType.Peak
    };
}
