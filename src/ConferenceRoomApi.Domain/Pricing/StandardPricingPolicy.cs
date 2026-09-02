using ConferenceRoomApi.Domain.Common.Exceptions;

namespace ConferenceRoomApi.Domain.Pricing;

/// <summary>
/// Splits a booking's [start, end) window at every rate-zone boundary it crosses and prices
/// each resulting sub-interval at that zone's multiplier, so a booking spanning e.g.
/// Standard → Peak → Standard is charged correctly instead of at a single blended rate.
/// </summary>
public sealed class StandardPricingPolicy : IPricingPolicy
{
    public RoomCostBreakdown Calculate(decimal basePricePerHour, TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new BusinessRuleViolationException("Booking end time must be after the start time.");
        }

        if (startTime < BusinessHours.OpensAt || endTime > BusinessHours.ClosesAt)
        {
            throw new BusinessRuleViolationException(
                $"Bookings are only accepted between {BusinessHours.OpensAt:HH:mm} and {BusinessHours.ClosesAt:HH:mm}.");
        }

        var boundaries = CollectBoundaries(startTime, endTime);
        var segments = new List<PriceSegment>(boundaries.Count - 1);

        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var segmentStart = boundaries[i];
            var segmentEnd = boundaries[i + 1];
            var zone = ResolveZone(Midpoint(segmentStart, segmentEnd));

            // Raw hours (e.g. a 20-minute segment = 1/3) is a repeating decimal; rounding
            // only the final money figure — not the intermediate hours — keeps the segment
            // cost accurate to the cent while avoiding compounding rounding error. Money
            // values are rounded here, not left to whatever precision the DB column happens
            // to apply, so the amount an API response shows always matches what gets persisted.
            var preciseHours = (decimal)(segmentEnd - segmentStart).Ticks / TimeSpan.TicksPerHour;
            var ratePerHour = Math.Round(basePricePerHour * zone.Multiplier, 2, MidpointRounding.AwayFromZero);
            var segmentCost = Math.Round(basePricePerHour * zone.Multiplier * preciseHours, 2, MidpointRounding.AwayFromZero);
            var hours = Math.Round(preciseHours, 2, MidpointRounding.AwayFromZero);

            segments.Add(new PriceSegment(zone.Type, segmentStart, segmentEnd, hours, zone.Multiplier, ratePerHour, segmentCost));
        }

        return new RoomCostBreakdown(segments, segments.Sum(s => s.SegmentCost));
    }

    private static List<TimeOnly> CollectBoundaries(TimeOnly startTime, TimeOnly endTime)
    {
        var boundaries = new SortedSet<TimeOnly> { startTime, endTime };

        foreach (var zone in RateZoneCatalog.Zones)
        {
            if (zone.Start > startTime && zone.Start < endTime)
            {
                boundaries.Add(zone.Start);
            }

            if (zone.End > startTime && zone.End < endTime)
            {
                boundaries.Add(zone.End);
            }
        }

        return boundaries.ToList();
    }

    private static TimeOnly Midpoint(TimeOnly start, TimeOnly end) => start.Add((end - start) / 2.0);

    private static RateZoneDefinition ResolveZone(TimeOnly instant)
    {
        RateZoneDefinition? resolved = null;

        // Later entries in the priority order win when zones overlap (Peak over Standard).
        foreach (var zoneType in RateZoneCatalog.PriorityOrder)
        {
            var zone = RateZoneCatalog.Zones.First(z => z.Type == zoneType);
            if (instant >= zone.Start && instant < zone.End)
            {
                resolved = zone;
            }
        }

        return resolved ?? throw new BusinessRuleViolationException(
            $"No rate zone covers {instant:HH:mm}; check BusinessHours against RateZoneCatalog.");
    }
}
