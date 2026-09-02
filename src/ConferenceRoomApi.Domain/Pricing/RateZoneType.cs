namespace ConferenceRoomApi.Domain.Pricing;

/// <summary>The named time-of-day pricing zones defined by the business.</summary>
public enum RateZoneType
{
    /// <summary>06:00–09:00, 10% discount.</summary>
    Morning,

    /// <summary>09:00–18:00 excluding Peak, base price.</summary>
    Standard,

    /// <summary>12:00–14:00, 15% surcharge. Takes precedence over Standard.</summary>
    Peak,

    /// <summary>18:00–23:00, 20% discount.</summary>
    Evening
}
