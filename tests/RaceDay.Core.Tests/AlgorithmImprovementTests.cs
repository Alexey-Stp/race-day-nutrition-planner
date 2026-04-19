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
        // Arrange — build a synthetic plan with intentionally excessive front-loading
        // and call EnforceFrontLoadConstraint directly via reflection.
        int durationMinutes = 240; // 4-hour race
        int windowEnd = (int)(durationMinutes * SchedulingConstraints.FrontLoadWindowFraction); // first 60 min

        var plan = new List<NutritionEvent>
        {
            // 200g in first 60 min — far exceeds 40% of 250g total
            new(TimeMin: 0,  Phase: RacePhase.Bike, PhaseDescription: "Bike", ProductName: "Bar", AmountPortions: 1, Action: "Eat", TotalCarbsSoFar: 80,  HasCaffeine: false, CaffeineMg: null, TotalCaffeineSoFar: 0, CarbsInEvent: 80),
            new(TimeMin: 20, Phase: RacePhase.Bike, PhaseDescription: "Bike", ProductName: "Bar", AmountPortions: 1, Action: "Eat", TotalCarbsSoFar: 160, HasCaffeine: false, CaffeineMg: null, TotalCaffeineSoFar: 0, CarbsInEvent: 80),
            new(TimeMin: 50, Phase: RacePhase.Bike, PhaseDescription: "Bike", ProductName: "Gel", AmountPortions: 1, Action: "Eat", TotalCarbsSoFar: 190, HasCaffeine: false, CaffeineMg: null, TotalCaffeineSoFar: 0, CarbsInEvent: 30),
            // 50g after window — must be kept
            new(TimeMin: 120, Phase: RacePhase.Bike, PhaseDescription: "Bike", ProductName: "Gel", AmountPortions: 1, Action: "Eat", TotalCarbsSoFar: 215, HasCaffeine: false, CaffeineMg: null, TotalCaffeineSoFar: 0, CarbsInEvent: 25),
            new(TimeMin: 180, Phase: RacePhase.Run,  PhaseDescription: "Run",  ProductName: "Gel", AmountPortions: 1, Action: "Eat", TotalCarbsSoFar: 240, HasCaffeine: false, CaffeineMg: null, TotalCaffeineSoFar: 0, CarbsInEvent: 25),
        };

        // Act — call private static method via reflection
        var method = typeof(PlanGenerator).GetMethod(
            "EnforceFrontLoadConstraint",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull(); // guard: method must exist
        method.Invoke(null, [plan, durationMinutes]);

        // Assert — constraint respected
        var totalCarbs = plan.Sum(e => e.CarbsInEvent);
        var frontCarbs = plan.Where(e => e.TimeMin <= windowEnd && e.SipMl == null).Sum(e => e.CarbsInEvent);

        if (totalCarbs > 0)
        {
            var ratio = frontCarbs / totalCarbs;
            (ratio <= SchedulingConstraints.MaxFrontLoadFraction).ShouldBeTrue(
                $"Front-load ratio {ratio:P1} still exceeds {SchedulingConstraints.MaxFrontLoadFraction:P0} after enforcement. " +
                $"Front: {frontCarbs}g / Total: {totalCarbs}g");
        }

        // Events after the window must all be preserved
        (plan.Any(e => e.TimeMin > windowEnd)).ShouldBeTrue(
            "Events after front-load window should not be removed");
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
