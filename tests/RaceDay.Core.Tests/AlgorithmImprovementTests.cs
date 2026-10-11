namespace RaceDay.Core.Tests;

using Xunit;
using RaceDay.Core.Services;
using RaceDay.Core.Models;
using RaceDay.Core.Constants;
using System.Linq;

public class AlgorithmImprovementTests
{
    private readonly PlanGenerator _generator = new();

    [Fact]
    public void GeneratePlan_RespectsClusterWindow()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - No items should be within 5 minutes of each other
        for (int i = 0; i < plan.Count - 1; i++)
        {
            var timeDiff = plan[i + 1].TimeMin - plan[i].TimeMin;
            (timeDiff >= SchedulingConstraints.ClusterWindow).ShouldBeTrue(
                $"Items at {plan[i].TimeMin}min and {plan[i + 1].TimeMin}min are too close ({timeDiff}min < {SchedulingConstraints.ClusterWindow}min)");
        }
    }

    [Fact]
    public void GeneratePlan_UsesDrinksWhenAvailable()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Bike, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateProductsWithDrinks();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Should include drink products
        var drinkItems = plan.Where(e => products.FirstOrDefault(p => p.Name == e.ProductName)?.Texture == ProductTexture.Drink).ToList();
        drinkItems.ShouldNotBeEmpty();
        
        // Drinks should provide significant portion of carbs
        var drinkCarbs = drinkItems.Sum(e => products.FirstOrDefault(p => p.Name == e.ProductName)?.CarbsG ?? 0);
        var totalCarbs = plan.Last().TotalCarbsSoFar;
        var drinkPercent = drinkCarbs / totalCarbs;
        
        (drinkPercent >= 0.2).ShouldBeTrue($"Drinks should provide at least 20% of carbs, actual: {drinkPercent:P0}");
    }

    [Fact]
    public void GeneratePlan_WithCaffeineEnabled_IncludesCaffeineAfter40Percent()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateProductsWithCaffeine();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products, caffeineEnabled: true);

        // Assert
        var caffeineItems = plan.Where(e => e.HasCaffeine && e.CaffeineMg > 0).ToList();
        
        if (caffeineItems.Any())
        {
            // All caffeine items should be after 40% of race
            var raceDurationMin = race.DurationHours * 60;
            var minCaffeineTime = raceDurationMin * SchedulingConstraints.CaffeinePreferredStartPercent;
            
            foreach (var item in caffeineItems)
            {
                (item.TimeMin >= minCaffeineTime).ShouldBeTrue(
                    $"Caffeine item at {item.TimeMin}min is before preferred start at {minCaffeineTime}min");
            }
        }
    }

    [Fact]
    public void GeneratePlan_WithCaffeineDisabled_NoCaffeineProducts()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateProductsWithCaffeine();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products, caffeineEnabled: false);

        // Assert
        var totalCaffeine = plan.Where(e => e.HasCaffeine).Sum(e => e.CaffeineMg ?? 0);
        totalCaffeine.ShouldBe(0);
    }

    [Fact]
    public void GeneratePlan_TriathlonBikePhaseDominance()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 4, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateProductsWithDrinks();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Bike should have more carbs than run
        var bikeCarbs = plan.Where(e => e.Phase == RacePhase.Bike)
            .Sum(e => products.FirstOrDefault(p => p.Name == e.ProductName)?.CarbsG ?? 0);
        var runCarbs = plan.Where(e => e.Phase == RacePhase.Run)
            .Sum(e => products.FirstOrDefault(p => p.Name == e.ProductName)?.CarbsG ?? 0);

        (bikeCarbs > runCarbs).ShouldBeTrue(
            $"Bike phase should have more carbs than run. Bike: {bikeCarbs}g, Run: {runCarbs}g");
        
        // Should be roughly 70/30 split (with some tolerance)
        var totalPhaseCarbs = bikeCarbs + runCarbs;
        if (totalPhaseCarbs > 0)
        {
            var bikeRatio = bikeCarbs / totalPhaseCarbs;
            (bikeRatio >= 0.55).ShouldBeTrue($"Bike should have at least 55% of carbs, actual: {bikeRatio:P0}");
        }
    }

    [Fact]
    public void GeneratePlan_ProductDiversity_NoExcessiveRepetition()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateVariedProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - No single product should dominate if alternatives exist
        var productCounts = plan.GroupBy(e => e.ProductName)
            .ToDictionary(g => g.Key, g => g.Count());

        if (productCounts.Count > 1 && plan.Count > 5)
        {
            var mostUsed = productCounts.Values.Max();
            var mostUsedPercent = (double)mostUsed / plan.Count;
            
            (mostUsedPercent < 0.75).ShouldBeTrue(
                $"Single product used {mostUsedPercent:P0} of time - diversity should be better");
        }
    }

    [Fact]
    public void BuildSlots_Triathlon_ProducesEventsInBothBikeAndRunPhases()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 4, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateProductsWithDrinks();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert — plan is generated successfully with events in both sport-specific phases.
        // The slot intervals used per phase (CyclingSlotIntervalMin for Bike, RunningSlotIntervalMin for Run)
        // drive the base schedule density, so both phases must be populated.
        var bikeEvents = plan.Where(e => e.Phase == RacePhase.Bike).ToList();
        var runEvents = plan.Where(e => e.Phase == RacePhase.Run).ToList();

        bikeEvents.ShouldNotBeEmpty();
        runEvents.ShouldNotBeEmpty();

        // Drink events must respect MinDrinkSpacing within each phase
        var bikeDrinks = bikeEvents.Where(e => e.SipMl != null).OrderBy(e => e.TimeMin).ToList();
        for (int i = 1; i < bikeDrinks.Count; i++)
        {
            var gap = bikeDrinks[i].TimeMin - bikeDrinks[i - 1].TimeMin;
            (gap >= SchedulingConstraints.SipIntervalMinutes).ShouldBeTrue(
                $"Bike sip events at {bikeDrinks[i - 1].TimeMin}min and {bikeDrinks[i].TimeMin}min are closer than {SchedulingConstraints.SipIntervalMinutes}min");
        }

        var runDrinks = runEvents.Where(e => e.SipMl != null).OrderBy(e => e.TimeMin).ToList();
        for (int i = 1; i < runDrinks.Count; i++)
        {
            var gap = runDrinks[i].TimeMin - runDrinks[i - 1].TimeMin;
            (gap >= SchedulingConstraints.SipIntervalMinutes).ShouldBeTrue(
                $"Run sip events at {runDrinks[i - 1].TimeMin}min and {runDrinks[i].TimeMin}min are closer than {SchedulingConstraints.SipIntervalMinutes}min");
        }
    }

    [Fact]
    public void EnforceFrontLoadConstraint_ExcessiveFrontLoading_ConstraintRespected()
    {
        // Arrange — a synthetic plan with intentionally excessive front-loading, fed straight into
        // the planner's final front-load pass (internal API, previously invoked via reflection).
        var race = new RaceProfile(SportType.Bike, DurationHours: 4, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var bar = new ProductEnhanced("Bar", 80, ProductTexture.Bake, false, 0);
        var gel30 = new ProductEnhanced("Gel 30", 30, ProductTexture.Gel, false, 0);
        var gel25 = new ProductEnhanced("Gel 25", 25, ProductTexture.Gel, false, 0);
        var products = new List<ProductEnhanced> { bar, gel30, gel25 };
        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, new AthleteProfile(75));
        var ctx = new PlanningContext(race, products, caffeineEnabled: false, legModel: null, targets);
        int windowEnd = ctx.FrontLoadWindowEndMin; // first 60 min

        // 190g in first 60 min — far exceeds 40% of the 240g total
        var plan = new List<PlannedEvent>
        {
            NutritionEventFactory.Intake(bar, 10, RacePhase.Bike),
            NutritionEventFactory.Intake(bar, 35, RacePhase.Bike),
            NutritionEventFactory.Intake(gel30, 55, RacePhase.Bike),
            // 50g after window — must be kept
            NutritionEventFactory.Intake(gel25, 120, RacePhase.Bike),
            NutritionEventFactory.Intake(gel25, 180, RacePhase.Bike),
        };
        double totalBefore = plan.Sum(e => e.Carbs);

        // Act
        PlanGenerator.EnforceFrontLoadConstraint(plan, ctx);

        // Assert — constraint respected
        var totalCarbs = plan.Sum(e => e.Carbs);
        var frontCarbs = plan.Where(e => e.TimeMin >= 0 && e.TimeMin <= windowEnd).Sum(e => e.Carbs);
        var ratio = frontCarbs / totalCarbs;
        (ratio <= SchedulingConstraints.MaxFrontLoadFraction).ShouldBeTrue(
            $"Front-load ratio {ratio:P1} still exceeds {SchedulingConstraints.MaxFrontLoadFraction:P0} after enforcement. " +
            $"Front: {frontCarbs}g / Total: {totalCarbs}g");

        // Events are moved later rather than deleted, so total carbs are unchanged
        totalCarbs.ShouldBe(totalBefore);

        // Events after the window must all be preserved
        plan.Count(e => e.TimeMin == 120 || e.TimeMin == 180).ShouldBe(2);
    }

    // ── Review findings: reproductions (A1–A11) ─────────────────────────────

    private static readonly AthleteProfile Athlete75 = new(WeightKg: 75);

    private static PlanResult Generate(RaceProfile race, List<ProductEnhanced> products, bool caffeine = false) =>
        new PlanGenerator().GeneratePlanWithDiagnostics(race, Athlete75, products, caffeine);

    [Fact]
    public void GeneratePlan_Running3hEasyOnlyGel160_StaysWithinTargetAndSpacing()
    {
        // A1: coverage phase used to append a 40 g gel every 10 min to the finish (~560 g vs 150 g).
        var race = new RaceProfile(SportType.Run, 3, TemperatureCondition.Moderate, IntensityLevel.Easy);
        var products = new List<ProductEnhanced> { new("Maurten GEL 160", 40, ProductTexture.Gel, false, 0, 65) };

        var result = Generate(race, products);

        PlanInvariants.AssertCarbsWithinTolerance(result, 150);
        PlanInvariants.AssertPairwiseSpacing(result.Events, products);
    }

    [Fact]
    public void GeneratePlan_GelsOnlyRun_RespectsMinimumGelSpacing()
    {
        // A2: GetMinimumSpacing had no callers; FindAvailableTimeSlot fell back to "first free minute".
        var race = new RaceProfile(SportType.Run, 3, TemperatureCondition.Moderate, IntensityLevel.Hard);
        var products = new List<ProductEnhanced>
        {
            new("Gel 25", 25, ProductTexture.Gel, false, 0),
            new("Light Gel", 20, ProductTexture.LightGel, false, 0)
        };

        var result = Generate(race, products);

        PlanInvariants.AssertPairwiseSpacing(result.Events, products);
        PlanInvariants.AssertMaxIntakesPerHour(result.Events);
    }

    [Fact]
    public void GeneratePlan_CaffeineEnabled_CaffeineEventsAtLeast45MinApart()
    {
        // A3: NextCaffeineHour was written but never read.
        var race = new RaceProfile(SportType.Run, 4, TemperatureCondition.Moderate, IntensityLevel.Hard);
        var products = new List<ProductEnhanced>
        {
            new("Caf Gel", 25, ProductTexture.Gel, true, 75),
            new("Plain Gel", 25, ProductTexture.Gel, false, 0)
        };

        var result = Generate(race, products, caffeine: true);

        result.Events.ShouldContain(e => e.HasCaffeine);
        PlanInvariants.AssertCaffeineRules(result.Events, true, result.Targets!.CaffeineMg);
    }

    [Fact]
    public void GeneratePlan_CaffeineDisabledWithCaffeinatedDrink_NoCaffeineInPlan()
    {
        // A3: the sip backbone used caffeinated drinks even when caffeine was disabled.
        var race = new RaceProfile(SportType.Bike, 3, TemperatureCondition.Moderate, IntensityLevel.Moderate);
        var products = new List<ProductEnhanced>
        {
            new("Caf Drink Mix", 40, ProductTexture.Drink, true, 100, 500),
            new("Plain Gel", 25, ProductTexture.Gel, false, 0)
        };

        var result = Generate(race, products, caffeine: false);

        PlanInvariants.AssertCaffeineRules(result.Events, false, 0);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void GeneratePlan_CaffeinatedProducts_EventsAlwaysCarryCaffeineFields()
    {
        // A3: events built by reconcile/coverage phases dropped HasCaffeine/CaffeineMg.
        var race = new RaceProfile(SportType.Run, 4, TemperatureCondition.Moderate, IntensityLevel.Hard);
        var products = new List<ProductEnhanced>
        {
            new("Caf Gel 40", 40, ProductTexture.Gel, true, 100),
            new("Plain Gel", 25, ProductTexture.Gel, false, 0)
        };

        var result = Generate(race, products, caffeine: true);

        PlanInvariants.AssertCaffeineFieldsMatchProducts(result.Events, products);
        PlanInvariants.AssertCaffeineRules(result.Events, true, result.Targets!.CaffeineMg);
    }

    [Theory]
    [InlineData(SportType.Run, 4.0, IntensityLevel.Hard)]
    [InlineData(SportType.Bike, 6.0, IntensityLevel.Moderate)]
    [InlineData(SportType.Triathlon, 5.0, IntensityLevel.Hard)]
    [InlineData(SportType.Run, 1.0, IntensityLevel.Moderate)]
    public void GeneratePlan_FinalPlan_RespectsFrontLoadRule(SportType sport, double hours, IntensityLevel intensity)
    {
        // A4: EnforceFrontLoadConstraint was a near no-op and later phases re-front-loaded from t=10.
        var race = new RaceProfile(sport, hours, TemperatureCondition.Moderate, intensity);

        var result = Generate(race, CreateProductsWithDrinks());

        PlanInvariants.AssertFrontLoadRule(result.Events, PlanInvariants.ToMinutes(hours));
    }

    [Fact]
    public void GeneratePlan_ShortCyclingChewsOnly_NoEventAfterFinish()
    {
        // A5: FindAvailableTimeSlot returned startMin+5 without checking endMin.
        var race = new RaceProfile(SportType.Bike, 0.2, TemperatureCondition.Moderate, IntensityLevel.Moderate);
        var products = new List<ProductEnhanced> { new("Chews", 24, ProductTexture.Chew, false, 0) };

        var result = Generate(race, products);

        PlanInvariants.AssertWithinRaceBounds(result.Events, PlanInvariants.ToMinutes(0.2));
    }

    [Theory]
    [InlineData(SportType.Run, 3.0)]
    [InlineData(SportType.Bike, 4.0)]
    [InlineData(SportType.Triathlon, 5.0)]
    public void GeneratePlan_SipClassification_KindActionAndSipMlAgree(SportType sport, double hours)
    {
        // A7: "Sip" was classified by Action in some places and by SipMl in others.
        var race = new RaceProfile(sport, hours, TemperatureCondition.Moderate, IntensityLevel.Hard);

        var result = Generate(race, CreateProductsWithDrinks());

        result.Events.ShouldContain(e => e.Kind == EventKind.Sip);
        PlanInvariants.AssertSipClassificationConsistent(result.Events);
    }

    [Fact]
    public void GeneratePlan_LightGelsOnlyBike_AtMostFourIntakesPerHour()
    {
        // A8: MaxIntakesPerHour was only a scoring nudge, never enforced.
        var race = new RaceProfile(SportType.Bike, 3, TemperatureCondition.Moderate, IntensityLevel.Hard);
        var products = new List<ProductEnhanced> { new("Light Gel", 20, ProductTexture.LightGel, false, 0) };

        var result = Generate(race, products);

        PlanInvariants.AssertMaxIntakesPerHour(result.Events);
        PlanInvariants.AssertPairwiseSpacing(result.Events, products);
        PlanInvariants.AssertCarbsWithinTolerance(result, result.Targets!.CarbsG);
    }

    [Theory]
    [InlineData(SportType.Run, 4.0)]
    [InlineData(SportType.Bike, 5.0)]
    public void GeneratePlan_Overshoot_KeepsLateRaceFuelling(SportType sport, double hours)
    {
        // A9: overshoot trimming removed events from the END of the race.
        var race = new RaceProfile(sport, hours, TemperatureCondition.Moderate, IntensityLevel.Easy);
        var products = new List<ProductEnhanced>
        {
            new("Big Gel", 45, ProductTexture.Gel, false, 0),
            new("Drink Mix", 80, ProductTexture.Drink, false, 0, 500)
        };

        var result = Generate(race, products);

        int duration = PlanInvariants.ToMinutes(hours);
        double total = result.Events.Sum(e => e.CarbsInEvent);
        double lastQuarter = result.Events.Where(e => e.TimeMin >= duration * 3 / 4).Sum(e => e.CarbsInEvent);
        (lastQuarter >= total * 0.10).ShouldBeTrue($"Only {lastQuarter:F0}g of {total:F0}g in the final quarter");
        PlanInvariants.AssertCarbsWithinTolerance(result, result.Targets!.CarbsG);
    }

    [Fact]
    public void GeneratePlanWithDiagnostics_PopulatesTargetsUsedForThePlan()
    {
        // A10: the API recomputed targets because PlanResult did not carry them.
        var race = new RaceProfile(SportType.Triathlon, 5, TemperatureCondition.Hot, IntensityLevel.Hard);

        var result = Generate(race, CreateProductsWithDrinks(), caffeine: true);

        var expected = NutritionCalculator.CalculateMultiNutrientTargets(race, Athlete75, caffeineEnabled: true);
        result.Targets.ShouldNotBeNull();
        result.Targets!.CarbsG.ShouldBe(expected.CarbsG);
        result.Targets.CaffeineMg.ShouldBe(expected.CaffeineMg);
        result.Targets.SegmentTargets.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(4.1, 246)]
    [InlineData(0.2, 12)]
    [InlineData(2.0, 120)]
    public void ToMinutes_FractionalHours_RoundsInsteadOfTruncating(double hours, int expected)
    {
        // A11: (int)(4.1 * 60) == 245 because 4.1 * 60 == 245.99999999999997.
        PlanGenerator.ToMinutes(hours).ShouldBe(expected);
    }

    [Fact]
    public void GeneratePlan_FractionalDuration_CoverageUsesRoundedMinutes()
    {
        // A11: coverage / bounds must use the rounded duration (246 min), not the truncated 245.
        var race = new RaceProfile(SportType.Run, 4.1, TemperatureCondition.Moderate, IntensityLevel.Moderate);

        var result = Generate(race, CreateProductsWithDrinks());

        int lastEvent = result.Events.Max(e => e.TimeMin);
        (lastEvent >= (int)(246 * SchedulingConstraints.MinCoverageRatio)).ShouldBeTrue($"Last event at {lastEvent}min");
        PlanInvariants.AssertWithinRaceBounds(result.Events, 246);
    }

    private List<ProductEnhanced> CreateTestProducts()
    {
        return new List<ProductEnhanced>
        {
            new("Energy Bar", 40, ProductTexture.Bake, false, 0, 0, "", 100),
            new("Gel", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Light Gel", 20, ProductTexture.LightGel, false, 0, 0, "", 30)
        };
    }

    private List<ProductEnhanced> CreateProductsWithDrinks()
    {
        return new List<ProductEnhanced>
        {
            new("High Carb Drink", 45, ProductTexture.Drink, false, 0, 500, "Energy", 300),
            new("Sports Drink", 35, ProductTexture.Drink, false, 0, 500, "Energy", 200),
            new("Energy Bar", 40, ProductTexture.Bake, false, 0, 0, "", 100),
            new("Gel", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Isotonic Gel", 22, ProductTexture.Gel, false, 0, 0, "Isotonic", 60)
        };
    }

    private List<ProductEnhanced> CreateProductsWithCaffeine()
    {
        return new List<ProductEnhanced>
        {
            new("Energy Bar", 40, ProductTexture.Bake, false, 0, 0, "", 100),
            new("Gel", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Caffeine Gel", 30, ProductTexture.Gel, true, 75, 0, "", 60),
            new("High Carb Drink", 45, ProductTexture.Drink, false, 0, 500, "Energy", 300)
        };
    }

    private List<ProductEnhanced> CreateVariedProducts()
    {
        return new List<ProductEnhanced>
        {
            new("Energy Bar A", 40, ProductTexture.Bake, false, 0, 0, "", 100),
            new("Gel A", 30, ProductTexture.Gel, false, 0, 0, "", 60),
            new("Gel B", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Light Gel A", 22, ProductTexture.LightGel, false, 0, 0, "", 45),
            new("Light Gel B", 20, ProductTexture.LightGel, false, 0, 0, "", 40),
            new("Chew", 22, ProductTexture.Chew, false, 0, 0, "", 55)
        };
    }
}
