namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Exceptions;

public class TriathlonLegModelTests
{
    // ─── Explicit legs ────────────────────────────────────────────────

    [Fact]
    public void Resolve_ExplicitLegs_UsedVerbatim()
    {
        var legs = new TriathlonLegs(SwimH: 1.1, BikeH: 5.8, RunH: 3.6);

        var model = TriathlonLegModel.Resolve(10.5, legs, preset: null, strict: false, out var warnings);

        model.SwimH.ShouldBe(1.1);
        model.BikeH.ShouldBe(5.8);
        model.RunH.ShouldBe(3.6);
        model.SwimEndH.ShouldBe(1.1, tolerance: 1e-9);
        model.BikeEndH.ShouldBe(6.9, tolerance: 1e-9);
        model.RunEndH.ShouldBe(10.5, tolerance: 1e-9);
        warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_ExplicitLegsWithinTolerance_NoWarning()
    {
        // Sum 10.53 vs duration 10.5 — within SumToleranceHours (0.05).
        var legs = new TriathlonLegs(SwimH: 1.1, BikeH: 5.8, RunH: 3.63);

        var model = TriathlonLegModel.Resolve(10.5, legs, preset: null, strict: false, out var warnings);

        warnings.ShouldBeEmpty();
        model.RunH.ShouldBe(3.63);
    }

    // ─── Presets ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(DistancePreset.Sprint, 0.17)]
    [InlineData(DistancePreset.Olympic, 0.15)]
    [InlineData(DistancePreset.HalfIron, 0.10)]
    [InlineData(DistancePreset.Ironman, 0.09)]
    public void Resolve_Preset_ExpandsToFractions(DistancePreset preset, double expectedSwimFraction)
    {
        double duration = 12.0;

        var model = TriathlonLegModel.Resolve(duration, legs: null, preset, strict: false, out var warnings);

        warnings.ShouldBeEmpty();
        (model.SwimH / duration).ShouldBe(expectedSwimFraction, tolerance: 1e-9);
        model.RunEndH.ShouldBe(duration, tolerance: 1e-9);
    }

    [Fact]
    public void Resolve_IronmanPreset_SwimIsEightToTenPercent()
    {
        var model = TriathlonLegModel.Resolve(12.0, legs: null, DistancePreset.Ironman, strict: false, out _);

        double swimFraction = model.SwimH / model.RunEndH;
        swimFraction.ShouldBeInRange(0.08, 0.10);
        swimFraction.ShouldBeLessThan(0.20); // definitively not the legacy default
    }

    // ─── Legacy default ───────────────────────────────────────────────

    [Fact]
    public void Resolve_NoLegsNoPreset_Uses20_50_30Default()
    {
        double duration = 10.0;

        var model = TriathlonLegModel.Resolve(duration, legs: null, preset: null, strict: false, out var warnings);

        warnings.ShouldBeEmpty();
        model.SwimH.ShouldBe(2.0, tolerance: 1e-9);
        model.BikeH.ShouldBe(5.0, tolerance: 1e-9);
        model.RunH.ShouldBe(3.0, tolerance: 1e-9);
        model.BikeCarbRatio.ShouldBe(0.70);
        model.RunCarbRatio.ShouldBe(0.30);
    }

    // ─── Normalisation (lenient) ──────────────────────────────────────

    [Fact]
    public void Resolve_LegsSumMismatch_Lenient_NormalisesAndWarns()
    {
        // Sum 9 vs duration 10.
        var legs = new TriathlonLegs(SwimH: 1, BikeH: 5, RunH: 3);

        var model = TriathlonLegModel.Resolve(10.0, legs, preset: null, strict: false, out var warnings);

        model.RunEndH.ShouldBe(10.0, tolerance: 1e-9);
        model.SwimH.ShouldBe(1.0 * 10.0 / 9.0, tolerance: 1e-9);
        model.BikeH.ShouldBe(5.0 * 10.0 / 9.0, tolerance: 1e-9);
        model.RunH.ShouldBe(3.0 * 10.0 / 9.0, tolerance: 1e-9);

        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("9");
        warnings[0].ShouldContain("10");
        warnings[0].ShouldContain("normalised");
    }

    // ─── Normalisation (strict) ───────────────────────────────────────

    [Fact]
    public void Resolve_LegsSumMismatch_Strict_Throws()
    {
        var legs = new TriathlonLegs(SwimH: 1, BikeH: 5, RunH: 3);

        Should.Throw<ValidationException>(() =>
            TriathlonLegModel.Resolve(10.0, legs, preset: null, strict: true, out _));
    }

    // ─── Invalid input ────────────────────────────────────────────────

    [Fact]
    public void Resolve_NegativeLeg_Throws()
    {
        var legs = new TriathlonLegs(SwimH: -1, BikeH: 5, RunH: 3);

        Should.Throw<ValidationException>(() =>
            TriathlonLegModel.Resolve(7.0, legs, preset: null, strict: false, out _));
    }

    [Fact]
    public void Resolve_ZeroDuration_Throws()
    {
        Should.Throw<ValidationException>(() =>
            TriathlonLegModel.Resolve(0.0, legs: null, preset: null, strict: false, out _));
    }

    [Fact]
    public void CarbRatios_SingleSourced_MatchSchedulingConstraints()
    {
        TriathlonLegModel.BikeCarbRatio.ShouldBe(Constants.SchedulingConstraints.TriathlonBikeCarbRatio);
        TriathlonLegModel.RunCarbRatio.ShouldBe(Constants.SchedulingConstraints.TriathlonRunCarbRatio);
    }
}
