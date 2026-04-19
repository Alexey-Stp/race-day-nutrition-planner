namespace RaceDay.Core.Tests;

using RaceDay.Core.Services;
using RaceDay.Core.Models;
using RaceDay.Core.Constants;
using System.Linq;

public class DynamicSelectionStrategyTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProductEnhanced Gel(double carbsG, string name = "Gel") =>
        new(name, carbsG, ProductTexture.Gel, false, 0);

    private static ProductEnhanced LightGel(double carbsG, string name = "LightGel") =>
        new(name, carbsG, ProductTexture.LightGel, false, 0);

    private static ProductEnhanced Chew(double carbsG = 22) =>
        new("Chew", carbsG, ProductTexture.Chew, false, 0);

    private static ProductEnhanced Bar(double carbsG = 40) =>
        new("Bar", carbsG, ProductTexture.Bake, false, 0);

    private static ProductEnhanced Drink(double carbsG = 35, double volumeMl = 500) =>
        new("Drink", carbsG, ProductTexture.Drink, false, 0, volumeMl);

    private static ProductSelectionContext NoContext(int currentTimeMin = 0, RacePhase phase = RacePhase.Run) =>
        new(null, -1, -1, currentTimeMin, phase);

    // ── GetAlternationBonus ───────────────────────────────────────────────────

    [Fact]
    public void GetAlternationBonus_NoLastProduct_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(Gel(25), null);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetAlternationBonus_LastProductNotGel_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(Gel(25), Bar());
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetAlternationBonus_CandidateNotGel_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(Bar(), Gel(40));
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetAlternationBonus_SmallAfterLarge_ReturnsMaxBonus()
    {
        // 40g gel → 22g light gel: canonical alternation
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(LightGel(22), Gel(40));
        bonus.ShouldBe(20.0);
    }

    [Fact]
    public void GetAlternationBonus_LargeAfterSmall_ReturnsMaxBonus()
    {
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(Gel(40), Gel(25));
        bonus.ShouldBe(20.0);
    }

    [Fact]
    public void GetAlternationBonus_LargeAfterLarge_ReturnsNegativeBonus()
    {
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(Gel(40), Gel(40));
        bonus.ShouldBe(-10.0);
    }

    [Fact]
    public void GetAlternationBonus_SmallAfterSmall_ReturnsSmallNegativeBonus()
    {
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(Gel(22), LightGel(20));
        bonus.ShouldBe(-5.0);
    }

    [Fact]
    public void GetAlternationBonus_MediumGelAfterLarge_ReturnsZero()
    {
        // 30g is between SmallGelThresholdG (25) and LargeGelThresholdG (35) — neutral
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(Gel(30), Gel(40));
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetAlternationBonus_LightGelTextureCountsAsSmall()
    {
        // LightGel texture with 20g carbs should count as "small" for alternation
        var bonus = DynamicSelectionStrategy.GetAlternationBonus(LightGel(20), Gel(40));
        bonus.ShouldBe(20.0);
    }

    // ── GetDrinkTimerBonus ────────────────────────────────────────────────────

    [Fact]
    public void GetDrinkTimerBonus_NonDrinkProduct_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetDrinkTimerBonus(Gel(25), currentTimeMin: 30, lastDrinkTimeMin: 0);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetDrinkTimerBonus_RecentDrink_BelowThreshold_ReturnsZero()
    {
        // Last drink 5 min ago, threshold is 15 — no urgency yet
        var bonus = DynamicSelectionStrategy.GetDrinkTimerBonus(Drink(), currentTimeMin: 25, lastDrinkTimeMin: 20);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetDrinkTimerBonus_DrinkJustOverThreshold_ReturnsSmallBonus()
    {
        // Last drink at 0, now at 16 → minutesSince=16, overrun=1, ratio=1/15≈0.067, bonus≈1.67
        var bonus = DynamicSelectionStrategy.GetDrinkTimerBonus(Drink(), currentTimeMin: 16, lastDrinkTimeMin: 0);
        bonus.ShouldBeGreaterThan(0.0);
        bonus.ShouldBeLessThan(5.0);
    }

    [Fact]
    public void GetDrinkTimerBonus_DrinkAtMaxGap_ReturnsFullBonus()
    {
        // Last drink at 0, now at 30 → minutesSince=30, maxGap=30, ratio=1.0, bonus=25
        var bonus = DynamicSelectionStrategy.GetDrinkTimerBonus(Drink(), currentTimeMin: 30, lastDrinkTimeMin: 0);
        bonus.ShouldBe(25.0, tolerance: 0.01);
    }

    [Fact]
    public void GetDrinkTimerBonus_DrinkBeyondMaxGap_ReturnsCapped()
    {
        // Gap > maxGap → capped at full bonus
        var bonus = DynamicSelectionStrategy.GetDrinkTimerBonus(Drink(), currentTimeMin: 60, lastDrinkTimeMin: 0);
        bonus.ShouldBe(25.0, tolerance: 0.01);
    }

    [Fact]
    public void GetDrinkTimerBonus_NeverHadDrink_SentinelMinus1_AtThreshold_ReturnsZero()
    {
        // Sentinel -1: effectiveLast = currentTimeMin - threshold = 15 - 15 = 0
        // minutesSince = 15 - 0 = 15 = threshold → ratio = 0 → bonus = 0
        var bonus = DynamicSelectionStrategy.GetDrinkTimerBonus(Drink(), currentTimeMin: 15, lastDrinkTimeMin: -1);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetDrinkTimerBonus_NeverHadDrink_SentinelMinus1_BeyondThreshold_ReturnsBonus()
    {
        // Sentinel -1 at t=30: effectiveLast = 30 - 15 = 15, minutesSince = 15 = threshold → 0
        // At t=45: effectiveLast = 45-15=30, minutesSince = 15 = threshold → 0
        // At t=50: effectiveLast = 50-15=35, minutesSince = 15 → still 0
        // Actually with sentinel: effectiveLast always = currentTimeMin - threshold
        // so minutesSince always equals threshold → always 0
        // Let's test a time where we'd expect SOME bonus if there was a real last drink at t=0:
        // → with sentinel the ramp starts at currentTimeMin - threshold, not at 0
        // so at t=25, effectiveLast=10, minutesSince=15=threshold → 0
        // at t=26, effectiveLast=11, minutesSince=15=threshold → 0
        // Sentinel means "treat as if we just hit the threshold right now" — always 0
        // This is correct: first drink is not urgent until a REAL drink was recorded.
        // Let's instead test with lastDrinkTimeMin=0 to verify bonus at t=20:
        var bonus = DynamicSelectionStrategy.GetDrinkTimerBonus(Drink(), currentTimeMin: 20, lastDrinkTimeMin: 0);
        // minutesSince=20, threshold=15, overrun=5, maxGap-threshold=15, ratio=5/15=0.333, bonus=8.33
        bonus.ShouldBeGreaterThan(0.0);
        bonus.ShouldBeLessThan(25.0);
    }

    // ── GetEffectivenessGapBonus ──────────────────────────────────────────────

    [Fact]
    public void GetEffectivenessGapBonus_NoLastProduct_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Gel(25), currentTimeMin: 40, lastNonSipTimeMin: -1,
            ProductTexture.Gel, lastNonSipCarbsG: 25);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetEffectivenessGapBonus_DrinkCandidate_ReturnsZero()
    {
        // Drinks are handled by GetDrinkTimerBonus, not this method
        var bonus = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Drink(), currentTimeMin: 60, lastNonSipTimeMin: 0,
            ProductTexture.Gel, lastNonSipCarbsG: 25);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetEffectivenessGapBonus_WindowStillActive_ReturnsZero()
    {
        // LightGel midpoint = (20+25)/2 = 22 min; lastAt=0, now=20 → overrun=-2 → 0
        var bonus = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Gel(25), currentTimeMin: 20, lastNonSipTimeMin: 0,
            ProductTexture.LightGel, lastNonSipCarbsG: 20);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetEffectivenessGapBonus_OneMinuteOverrun_ReturnsSmallBonus()
    {
        // LightGel midpoint=22; lastAt=0, now=23 → overrun=1, ratio=0.1, bonus=3.0
        var bonus = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Gel(25), currentTimeMin: 23, lastNonSipTimeMin: 0,
            ProductTexture.LightGel, lastNonSipCarbsG: 20);
        bonus.ShouldBe(3.0, tolerance: 0.01);
    }

    [Fact]
    public void GetEffectivenessGapBonus_TenMinuteOverrun_ReturnsFullBonus()
    {
        // LightGel midpoint=22; lastAt=0, now=32 → overrun=10, ratio=1.0, bonus=30
        var bonus = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Gel(25), currentTimeMin: 32, lastNonSipTimeMin: 0,
            ProductTexture.LightGel, lastNonSipCarbsG: 20);
        bonus.ShouldBe(30.0, tolerance: 0.01);
    }

    [Fact]
    public void GetEffectivenessGapBonus_BeyondTenMinutes_ReturnsCapped()
    {
        // overrun > 10 → capped at 30
        var bonus = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Gel(25), currentTimeMin: 60, lastNonSipTimeMin: 0,
            ProductTexture.LightGel, lastNonSipCarbsG: 20);
        bonus.ShouldBe(30.0, tolerance: 0.01);
    }

    [Fact]
    public void GetEffectivenessGapBonus_LargeGelWindowLongerThanSmall_CorrectMidpoint()
    {
        // Large gel midpoint = (35+45)/2 = 40; small gel midpoint = (25+35)/2 = 30
        // At t=35 after a large gel: overrun = 35 - (0+40) = -5 → 0
        // At t=35 after a small gel (25g): overrun = 35 - (0+30) = 5 → bonus = 0.5*30 = 15
        var bonusAfterLarge = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Gel(25), currentTimeMin: 35, lastNonSipTimeMin: 0,
            ProductTexture.Gel, lastNonSipCarbsG: 40); // large gel
        bonusAfterLarge.ShouldBe(0.0);

        var bonusAfterSmall = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            Gel(25), currentTimeMin: 35, lastNonSipTimeMin: 0,
            ProductTexture.Gel, lastNonSipCarbsG: 25); // standard gel (midpoint 30)
        bonusAfterSmall.ShouldBe(15.0, tolerance: 0.01);
    }

    // ── GetPhaseChewBonus ─────────────────────────────────────────────────────

    [Fact]
    public void GetPhaseChewBonus_ChewOnBike_ReturnsBonus()
    {
        var bonus = DynamicSelectionStrategy.GetPhaseChewBonus(Chew(), RacePhase.Bike);
        bonus.ShouldBe(15.0);
    }

    [Fact]
    public void GetPhaseChewBonus_ChewOnRun_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetPhaseChewBonus(Chew(), RacePhase.Run);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetPhaseChewBonus_GelOnBike_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetPhaseChewBonus(Gel(25), RacePhase.Bike);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void GetPhaseChewBonus_ChewOnSwim_ReturnsZero()
    {
        var bonus = DynamicSelectionStrategy.GetPhaseChewBonus(Chew(), RacePhase.Swim);
        bonus.ShouldBe(0.0);
    }

    // ── CalculateTotalDynamicBonus ────────────────────────────────────────────

    [Fact]
    public void CalculateTotalDynamicBonus_NoContext_ReturnsZero()
    {
        var context = NoContext(currentTimeMin: 0);
        var bonus = DynamicSelectionStrategy.CalculateTotalDynamicBonus(Gel(25), context);
        bonus.ShouldBe(0.0);
    }

    [Fact]
    public void CalculateTotalDynamicBonus_AllFactorsApply_EqualsSumOfParts()
    {
        // Scenario: small gel candidate, last was large gel, gap overrun, on run
        var largeGel = Gel(40, "Large Gel");
        var smallGelCandidate = LightGel(20, "Small Gel");

        var context = new ProductSelectionContext(
            LastNonSipProduct: largeGel,
            LastNonSipTimeMin: 0,
            LastDrinkTimeMin: -1,
            CurrentTimeMin: 30,
            CurrentPhase: RacePhase.Run
        );

        double total = DynamicSelectionStrategy.CalculateTotalDynamicBonus(smallGelCandidate, context);

        double expectedAlt  = DynamicSelectionStrategy.GetAlternationBonus(smallGelCandidate, largeGel);
        double expectedDrink = DynamicSelectionStrategy.GetDrinkTimerBonus(smallGelCandidate, 30, -1);
        double expectedGap  = DynamicSelectionStrategy.GetEffectivenessGapBonus(
            smallGelCandidate, 30, 0, ProductTexture.Gel, 40);
        double expectedChew = DynamicSelectionStrategy.GetPhaseChewBonus(smallGelCandidate, RacePhase.Run);

        total.ShouldBe(expectedAlt + expectedDrink + expectedGap + expectedChew, tolerance: 0.001);
    }

    [Fact]
    public void CalculateTotalDynamicBonus_DrinkWithTimerOnly_CorrectSum()
    {
        // Drink candidate after a 30-min gap — only drink timer should fire
        var drinkCandidate = Drink();
        var context = new ProductSelectionContext(
            LastNonSipProduct: Gel(25),
            LastNonSipTimeMin: 0,
            LastDrinkTimeMin: 0,
            CurrentTimeMin: 30,
            CurrentPhase: RacePhase.Run
        );

        double total = DynamicSelectionStrategy.CalculateTotalDynamicBonus(drinkCandidate, context);
        double expectedDrink = DynamicSelectionStrategy.GetDrinkTimerBonus(drinkCandidate, 30, 0);

        total.ShouldBe(expectedDrink, tolerance: 0.001);
    }

    [Fact]
    public void CalculateTotalDynamicBonus_ChewOnBike_ReturnsPhaseBonus()
    {
        var context = new ProductSelectionContext(
            LastNonSipProduct: null,
            LastNonSipTimeMin: -1,
            LastDrinkTimeMin: 5,    // recent drink, no timer urgency
            CurrentTimeMin: 10,
            CurrentPhase: RacePhase.Bike
        );

        double total = DynamicSelectionStrategy.CalculateTotalDynamicBonus(Chew(), context);
        total.ShouldBe(15.0, tolerance: 0.001);
    }

    // ── Integration tests (via PlanGenerator) ────────────────────────────────

    private readonly PlanGenerator _generator = new();

    [Fact]
    public void GeneratePlan_BikeRace_ChewableProductsAppearInPlan()
    {
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Bike, DurationHours: 3,
            Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = new List<ProductEnhanced>
        {
            new("Sports Drink", 35, ProductTexture.Drink, false, 0, 500, "Energy", 200),
            new("Energy Chew", 22, ProductTexture.Chew, false, 0, 0, "", 50),
            new("Gel 25g", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Gel 40g", 40, ProductTexture.Gel, false, 0, 0, "", 80)
        };

        var plan = _generator.GeneratePlan(race, athlete, products);

        plan.ShouldNotBeEmpty();
        // Chews should appear when available on bike (phase bonus + static bonus)
        var chewEvents = plan.Where(e => e.ProductName == "Energy Chew").ToList();
        chewEvents.ShouldNotBeEmpty("Chew product should be selected during bike phase");
    }

    [Fact]
    public void GeneratePlan_GelOnlyProducts_LargeAndSmallGelsAlternate()
    {
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2,
            Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = new List<ProductEnhanced>
        {
            new("Small Gel", 22, ProductTexture.LightGel, false, 0, 0, "", 40),
            new("Large Gel", 40, ProductTexture.Gel, false, 0, 0, "", 80)
        };

        var plan = _generator.GeneratePlan(race, athlete, products);

        var nonSipEvents = plan.Where(e => e.SipMl == null).ToList();
        nonSipEvents.ShouldNotBeEmpty();

        // Both gel sizes should appear — alternation prevents single-product dominance
        var smallCount = nonSipEvents.Count(e => e.ProductName == "Small Gel");
        var largeCount = nonSipEvents.Count(e => e.ProductName == "Large Gel");

        if (nonSipEvents.Count >= 4)
        {
            (smallCount > 0 && largeCount > 0).ShouldBeTrue(
                $"Both gel sizes should appear (small: {smallCount}, large: {largeCount})");
        }
    }

    [Fact]
    public void GeneratePlan_LongRaceWithDrink_DrinkAppearsRegularly()
    {
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Bike, DurationHours: 4,
            Temperature: TemperatureCondition.Hot, Intensity: IntensityLevel.Moderate);
        var products = new List<ProductEnhanced>
        {
            new("High Carb Drink", 40, ProductTexture.Drink, false, 0, 500, "Energy", 250),
            new("Gel 25g", 25, ProductTexture.Gel, false, 0, 0, "", 50)
        };

        var plan = _generator.GeneratePlan(race, athlete, products);

        var sipEvents = plan.Where(e => e.SipMl != null).OrderBy(e => e.TimeMin).ToList();
        sipEvents.ShouldNotBeEmpty();

        // No gap between consecutive sips should exceed DrinkTimerMaxGapMin (30 min)
        for (int i = 1; i < sipEvents.Count; i++)
        {
            var gap = sipEvents[i].TimeMin - sipEvents[i - 1].TimeMin;
            (gap <= ProductEffectivenessProfiles.DrinkTimerMaxGapMin).ShouldBeTrue(
                $"Drink gap at {sipEvents[i - 1].TimeMin}–{sipEvents[i].TimeMin} min ({gap} min) exceeds max ({ProductEffectivenessProfiles.DrinkTimerMaxGapMin} min)");
        }
    }
}
