using ConferenceRoomApi.Domain.Common.Exceptions;
using ConferenceRoomApi.Domain.Pricing;
using FluentAssertions;
using Xunit;

namespace ConferenceRoomApi.UnitTests.Pricing;

public class StandardPricingPolicyTests
{
    private readonly StandardPricingPolicy _policy = new();

    [Fact]
    public void Calculate_EntirelyWithinStandardHours_ChargesBaseRate()
    {
        var result = _policy.Calculate(2000m, new TimeOnly(10, 0), new TimeOnly(11, 0));

        result.TotalCost.Should().Be(2000m);
        result.Segments.Should().ContainSingle(s => s.Zone == RateZoneType.Standard);
    }

    [Fact]
    public void Calculate_EntirelyWithinMorningHours_Applies10PercentDiscount()
    {
        var result = _policy.Calculate(2000m, new TimeOnly(7, 0), new TimeOnly(8, 0));

        result.TotalCost.Should().Be(1800m);
        result.Segments.Should().ContainSingle(s => s.Zone == RateZoneType.Morning);
    }

    [Fact]
    public void Calculate_EntirelyWithinEveningHours_Applies20PercentDiscount()
    {
        var result = _policy.Calculate(2000m, new TimeOnly(19, 0), new TimeOnly(20, 0));

        result.TotalCost.Should().Be(1600m);
        result.Segments.Should().ContainSingle(s => s.Zone == RateZoneType.Evening);
    }

    [Fact]
    public void Calculate_EntirelyWithinPeakHours_Applies15PercentSurcharge()
    {
        var result = _policy.Calculate(2000m, new TimeOnly(12, 0), new TimeOnly(13, 0));

        result.TotalCost.Should().Be(2300m);
        result.Segments.Should().ContainSingle(s => s.Zone == RateZoneType.Peak);
    }

    [Fact]
    public void Calculate_ExactlyPeakWindow_DoesNotSplitIntoExtraSegments()
    {
        var result = _policy.Calculate(2000m, new TimeOnly(12, 0), new TimeOnly(14, 0));

        result.Segments.Should().ContainSingle();
        result.Segments.Single().Zone.Should().Be(RateZoneType.Peak);
        result.TotalCost.Should().Be(4600m); // 2h * 2000 * 1.15
    }

    [Fact]
    public void Calculate_SpanningStandardPeakStandard_PricesEachSegmentSeparately()
    {
        // Mirrors the TZ's own example: 01.09.2024, room A, 10:00–14:00.
        var result = _policy.Calculate(2000m, new TimeOnly(10, 0), new TimeOnly(14, 0));

        result.TotalCost.Should().Be(8600m); // 2h standard (4000) + 2h peak (4600)
        result.Segments.Should().HaveCount(2);
        result.Segments.Select(s => s.Zone).Should().Equal(RateZoneType.Standard, RateZoneType.Peak);
    }

    [Fact]
    public void Calculate_SpanningMorningIntoStandard_SplitsAtTheHalfHourBoundary()
    {
        var result = _policy.Calculate(2000m, new TimeOnly(8, 30), new TimeOnly(9, 30));

        result.Segments.Should().HaveCount(2);
        result.Segments[0].Zone.Should().Be(RateZoneType.Morning);
        result.Segments[0].SegmentCost.Should().Be(900m); // 0.5h * 2000 * 0.9
        result.Segments[1].Zone.Should().Be(RateZoneType.Standard);
        result.Segments[1].SegmentCost.Should().Be(1000m); // 0.5h * 2000
        result.TotalCost.Should().Be(1900m);
    }

    [Fact]
    public void Calculate_SpanningStandardIntoEvening_SplitsAtSixPm()
    {
        var result = _policy.Calculate(2000m, new TimeOnly(17, 30), new TimeOnly(18, 30));

        result.TotalCost.Should().Be(1800m); // 0.5h standard (1000) + 0.5h evening (800)
    }

    [Fact]
    public void Calculate_FullOperatingDay_SumsAllFiveSegmentsCorrectly()
    {
        var result = _policy.Calculate(2000m, BusinessHours.OpensAt, BusinessHours.ClosesAt);

        // Morning 3h*0.9*2000=5400, Standard(9-12) 3h*2000=6000, Peak 2h*2300=4600,
        // Standard(14-18) 4h*2000=8000, Evening 5h*0.8*2000=8000
        result.TotalCost.Should().Be(32000m);
        result.Segments.Should().HaveCount(5);
    }

    [Fact]
    public void Calculate_DurationNotAMultipleOfSixMinutes_RoundsCostToTheCent()
    {
        // 20 minutes = 1/3 hour, a repeating decimal — must not leak into the returned cost.
        var result = _policy.Calculate(1800m, new TimeOnly(10, 0), new TimeOnly(10, 20));

        result.Segments.Should().ContainSingle();
        var segment = result.Segments.Single();
        segment.SegmentCost.Should().Be(600.00m);
        decimal.GetBits(segment.SegmentCost)[3].Should().Be(decimal.GetBits(600.00m)[3], "scale must be exactly 2 decimal places");
        result.TotalCost.Should().Be(600.00m);
    }

    [Theory]
    [InlineData(9, 0, 9, 0)] // zero-length
    [InlineData(10, 0, 9, 0)] // inverted
    public void Calculate_EndNotAfterStart_Throws(int startH, int startM, int endH, int endM)
    {
        var act = () => _policy.Calculate(2000m, new TimeOnly(startH, startM), new TimeOnly(endH, endM));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Calculate_StartBeforeOpening_Throws()
    {
        var act = () => _policy.Calculate(2000m, new TimeOnly(5, 0), new TimeOnly(7, 0));

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Calculate_EndAfterClosing_Throws()
    {
        var act = () => _policy.Calculate(2000m, new TimeOnly(22, 0), new TimeOnly(23, 30));

        act.Should().Throw<BusinessRuleViolationException>();
    }
}
