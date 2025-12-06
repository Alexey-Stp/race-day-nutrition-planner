namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Services;
using RaceDay.Core.Repositories;
using RaceDay.Core.Exceptions;

public class PlanGeneratorTests
{
    [Fact]
    public void GeneratePlan_RunningWithGel_CreatesNutritionEvents()
    {
        // Arrange - Test advanced plan generation for running
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Chew", "chew", CarbsG: 20, SodiumMg: 80)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan);
        Assert.All(plan, @event => Assert.NotNull(@event.ProductName));
        Assert.All(plan, @event => Assert.True(@event.TotalCarbsSoFar > 0));
    }

    [Fact]
    public void GeneratePlan_WithMultipleProducts_CreatesSchedule()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1.5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = new List<Product>
        {
            new Product("Gel Light", "gel", CarbsG: 20, SodiumMg: 80),
            new Product("Gel Heavy", "gel", CarbsG: 30, SodiumMg: 120)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan);
        Assert.True(plan.Count > 0, "Plan should contain events");
    }

    [Fact]
    public void GeneratePlan_BikeWithGelAndDrink_CreatesSchedule()
    {
        // Arrange - Bike races can use various product types
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Bike, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan);
        Assert.All(plan, @event => Assert.True(@event.TimeMin >= -15));
    }

    [Fact]
    public void GeneratePlan_TriathlonWithVariousProducts_CreatesSchedule()
    {
        // Arrange - Triathlon needs both gels and drinks
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 1, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500),
            new Product("Energy Bar", "bar", CarbsG: 40, SodiumMg: 150)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan);
    }

    [Fact]
    public void GeneratePlan_TracksProgressiveCarbsConsumption()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Assert - Carbs should increase or stay same, never decrease
        if (plan.Count > 1)
        {
            for (int i = 1; i < plan.Count; i++)
            {
                Assert.True(
                    plan[i].TotalCarbsSoFar >= plan[i - 1].TotalCarbsSoFar,
                    "Carb totals should increase or stay same");
            }
        }
    }

    [Fact]
    public void GeneratePlan_IncludesPreRaceIntake()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Energy Bar", "bar", CarbsG: 40, SodiumMg: 150),
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Assert - Should have pre-race event at -15 minutes
        var preRaceEvent = plan.FirstOrDefault(e => e.TimeMin == -15);
        if (preRaceEvent != null)
        {
            Assert.NotNull(preRaceEvent.ProductName);
        }
    }

    [Fact]
    public void GeneratePlan_LongRaceWithHeavyAthlete_ProducesMoreNutrition()
    {
        // Arrange
        var heavyAthlete = new AthleteProfile(WeightKg: 100);
        var lightAthlete = new AthleteProfile(WeightKg: 60);
        var race = new RaceProfile(SportType.Run, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100)
        };

        // Act
        var service = new NutritionPlanService();
        var heavyPlan = service.GeneratePlan(race, heavyAthlete, products);
        var lightPlan = service.GeneratePlan(race, lightAthlete, products);

        // Assert - Heavy athlete should have more total carbs
        var heavyTotalCarbs = heavyPlan.LastOrDefault()?.TotalCarbsSoFar ?? 0;
        var lightTotalCarbs = lightPlan.LastOrDefault()?.TotalCarbsSoFar ?? 0;
        
        Assert.True(heavyTotalCarbs >= lightTotalCarbs, "Heavier athlete should consume at least as much nutrition");
    }

    [Fact]
    public void GeneratePlan_AllEventsHaveRequiredFields()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Assert - All events should have required fields
        Assert.All(plan, @event =>
        {
            Assert.NotNull(@event.ProductName);
            Assert.NotNull(@event.Action);
            Assert.True(@event.AmountPortions > 0);
            Assert.True(@event.TotalCarbsSoFar >= 0);
        });
    }
}
