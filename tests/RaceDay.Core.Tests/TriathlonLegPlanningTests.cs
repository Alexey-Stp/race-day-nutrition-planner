namespace RaceDay.Core.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.Core.Models;
using RaceDay.Core.Services;

/// <summary>
/// End-to-end scenario coverage for the configurable triathlon leg model (RAC-1).
/// </summary>
public class TriathlonLegPlanningTests
{
    private static readonly AthleteProfile Athlete = new(WeightKg: 75);

    private static List<Product> StandardProducts() => new()
    {
        new Product("Maurten GEL 100", "gel", CarbsG: 25, SodiumMg: 85, VolumeMl: 40),
        new Product("Maurten Drink Mix 320 (500ml)", "drink", CarbsG: 80, SodiumMg: 345, VolumeMl: 500),
        new Product("Energy Bar", "bar", CarbsG: 35, SodiumMg: 150, VolumeMl: 0)
    };

    // ─── Scenario 1: explicit legs drive the plan (Ironman happy path) ──

    [Fact]
    public void Scenario1_ExplicitLegs_SegmentTargetsMatchLegBoundaries()
    {
        var legs = new TriathlonLegs(SwimH: 1.1, BikeH: 5.8, RunH: 3.6);
        var race = new RaceProfile(SportType.Triathlon, 10.5, TemperatureCondition.Moderate,
            IntensityLevel.Hard, legs);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, Athlete);

        targets.SegmentTargets.ShouldNotBeNull();
        var seg = targets.SegmentTargets!;

        // One PhaseTargets per leg, durations matching the boundaries (minutes).
        seg[RacePhase.Swim].DurationMinutes.ShouldBe(1.1 * 60, tolerance: 1e-6);
        seg[RacePhase.Bike].DurationMinutes.ShouldBe(5.8 * 60, tolerance: 1e-6);
        seg[RacePhase.Run].DurationMinutes.ShouldBe(3.6 * 60, tolerance: 1e-6);

        // Swim carries zero fuelling; bike carries ~70% of total carbs.
        seg[RacePhase.Swim].CarbsG.ShouldBe(0);
        seg[RacePhase.Bike].CarbsG.ShouldBe(targets.CarbsG * 0.70, tolerance: 1e-6);
        seg[RacePhase.Run].CarbsG.ShouldBe(targets.CarbsG * 0.30, tolerance: 1e-6);
    }

    [Fact]
    public void Scenario1_ExplicitLegs_SwimPhaseHasNoScheduledFuelling()
    {
        var legs = new TriathlonLegs(SwimH: 1.1, BikeH: 5.8, RunH: 3.6);
        var race = new RaceProfile(SportType.Triathlon, 10.5, TemperatureCondition.Moderate,
            IntensityLevel.Hard, legs);

        var service = new NutritionPlanService(NullLogger<NutritionPlanService>.Instance);
        var plan = service.GeneratePlan(race, Athlete, StandardProducts());

        // No in-race swim-phase fuelling anywhere in the plan.
        plan.Count(e => e.Phase == RacePhase.Swim && e.TimeMin >= 0).ShouldBe(0);
    }

    // ─── Scenario 2: preset expands to sensible legs ───────────────────

    [Fact]
    public void Scenario2_IronmanPreset_SwimUnder20PercentAndFlowsThrough()
    {
        var race = new RaceProfile(SportType.Triathlon, 12.0, TemperatureCondition.Moderate,
            IntensityLevel.Hard, Legs: null, Preset: DistancePreset.Ironman);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, Athlete);
        var seg = targets.SegmentTargets!;

        double swimHours = seg[RacePhase.Swim].DurationMinutes / 60.0;
        (swimHours / 12.0).ShouldBeInRange(0.08, 0.10);

        // Boundaries flow identically: the model both calculators consume is the same.
        var model = TriathlonLegModel.Resolve(12.0, null, DistancePreset.Ironman, strict: false, out _);
        seg[RacePhase.Swim].DurationMinutes.ShouldBe(model.SwimH * 60, tolerance: 1e-9);
        seg[RacePhase.Bike].DurationMinutes.ShouldBe(model.BikeH * 60, tolerance: 1e-9);
        seg[RacePhase.Run].DurationMinutes.ShouldBe(model.RunH * 60, tolerance: 1e-9);
    }

    // ─── Scenario 3: single source of truth ────────────────────────────

    [Fact]
    public void Scenario3_SegmentDurations_AreExactlyResolvedModelValues()
    {
        // A deliberately non-default split: if either call site reintroduced a local
        // literal (e.g. 0.20/0.50/0.30), these equalities would fail.
        var legs = new TriathlonLegs(SwimH: 0.8, BikeH: 6.2, RunH: 4.0);
        var race = new RaceProfile(SportType.Triathlon, 11.0, TemperatureCondition.Moderate,
            IntensityLevel.Hard, legs);

        var model = TriathlonLegModel.Resolve(11.0, legs, null, strict: false, out _);
        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, Athlete);
        var seg = targets.SegmentTargets!;

        seg[RacePhase.Swim].DurationMinutes.ShouldBe(model.SwimH * 60);
        seg[RacePhase.Bike].DurationMinutes.ShouldBe(model.BikeH * 60);
        seg[RacePhase.Run].DurationMinutes.ShouldBe(model.RunH * 60);

        // The generated plan's bike-phase events must respect the resolved bike window,
        // not a 0.20/0.50-derived one.
        var service = new NutritionPlanService(NullLogger<NutritionPlanService>.Instance);
        var plan = service.GeneratePlan(race, Athlete, StandardProducts());
        int bikeStartMin = (int)(model.SwimEndH * 60);
        int bikeEndMin = (int)(model.BikeEndH * 60);
        foreach (var e in plan.Where(e => e.Phase == RacePhase.Bike && e.TimeMin >= 0))
        {
            e.TimeMin.ShouldBeGreaterThanOrEqualTo(bikeStartMin - 1);
            e.TimeMin.ShouldBeLessThanOrEqualTo(bikeEndMin + 1);
        }
    }

    // ─── Scenario 5: no behaviour change for legacy path ───────────────

    [Fact]
    public void Scenario5_LegacyPath_IdenticalToDefaultSplit()
    {
        var legacyRace = new RaceProfile(SportType.Triathlon, 4.5, TemperatureCondition.Moderate,
            IntensityLevel.Hard);
        var explicitDefaultRace = new RaceProfile(SportType.Triathlon, 4.5, TemperatureCondition.Moderate,
            IntensityLevel.Hard, new TriathlonLegs(4.5 * 0.20, 4.5 * 0.50, 4.5 * 0.30));

        var service = new NutritionPlanService(NullLogger<NutritionPlanService>.Instance);
        var products = StandardProducts();
        var planLegacy = service.GeneratePlan(legacyRace, Athlete, products);
        var planExplicit = service.GeneratePlan(explicitDefaultRace, Athlete, products);

        planLegacy.Count.ShouldBe(planExplicit.Count);
        for (int i = 0; i < planLegacy.Count; i++)
        {
            planLegacy[i].TimeMin.ShouldBe(planExplicit[i].TimeMin);
            planLegacy[i].ProductName.ShouldBe(planExplicit[i].ProductName);
            planLegacy[i].AmountPortions.ShouldBe(planExplicit[i].AmountPortions);
            planLegacy[i].Phase.ShouldBe(planExplicit[i].Phase);
        }
    }

    [Fact]
    public void Scenario5_LegacyPath_ProducesNoLegWarnings()
    {
        var race = new RaceProfile(SportType.Triathlon, 5.0, TemperatureCondition.Moderate, IntensityLevel.Hard);
        var service = new NutritionPlanService(NullLogger<NutritionPlanService>.Instance);

        var result = service.GeneratePlanWithDiagnostics(race, Athlete, StandardProducts());

        result.Warnings.ShouldNotContain(w => w.Contains("normalised") || w.Contains("ignored"));
    }

    // ─── Scenario 6: legs on a non-triathlon sport are ignored safely ──

    [Fact]
    public void Scenario6_LegsOnBike_IgnoredWithWarning_NoException()
    {
        var race = new RaceProfile(SportType.Bike, 3.0, TemperatureCondition.Moderate,
            IntensityLevel.Hard, new TriathlonLegs(0.5, 1.5, 1.0));

        var service = new NutritionPlanService(NullLogger<NutritionPlanService>.Instance);
        var result = service.GeneratePlanWithDiagnostics(race, Athlete, StandardProducts());

        result.Events.ShouldNotBeEmpty();
        result.Events.ShouldAllBe(e => e.Phase != RacePhase.Swim);
        result.Warnings.ShouldContain(w => w.Contains("ignored"));
    }

    // ─── Scenario 4: lenient normalisation emits a warning through diagnostics ──

    [Fact]
    public void Scenario4_LegSumMismatch_Lenient_EmitsWarning()
    {
        var race = new RaceProfile(SportType.Triathlon, 10.0, TemperatureCondition.Moderate,
            IntensityLevel.Hard, new TriathlonLegs(1, 5, 3)); // sum 9 != 10

        var service = new NutritionPlanService(NullLogger<NutritionPlanService>.Instance);
        var result = service.GeneratePlanWithDiagnostics(race, Athlete, StandardProducts());

        result.Warnings.ShouldContain(w => w.Contains("normalised"));
    }
}
