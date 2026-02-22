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
            Assert.True(timeDiff >= SchedulingConstraints.ClusterWindow, 
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
        Assert.NotEmpty(drinkItems);
        
        // Drinks should provide significant portion of carbs
        var drinkCarbs = drinkItems.Sum(e => products.FirstOrDefault(p => p.Name == e.ProductName)?.CarbsG ?? 0);
        var totalCarbs = plan.Last().TotalCarbsSoFar;
        var drinkPercent = drinkCarbs / totalCarbs;
        
        Assert.True(drinkPercent >= 0.2, $"Drinks should provide at least 20% of carbs, actual: {drinkPercent:P0}");
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
                Assert.True(item.TimeMin >= minCaffeineTime, 
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
        Assert.Equal(0, totalCaffeine);
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

        Assert.True(bikeCarbs > runCarbs, 
            $"Bike phase should have more carbs than run. Bike: {bikeCarbs}g, Run: {runCarbs}g");
        
        // Should be roughly 70/30 split (with some tolerance)
        var totalPhaseCarbs = bikeCarbs + runCarbs;
        if (totalPhaseCarbs > 0)
        {
            var bikeRatio = bikeCarbs / totalPhaseCarbs;
            Assert.True(bikeRatio >= 0.55, $"Bike should have at least 55% of carbs, actual: {bikeRatio:P0}");
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
            
            Assert.True(mostUsedPercent < 0.75, 
                $"Single product used {mostUsedPercent:P0} of time - diversity should be better");
        }
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
