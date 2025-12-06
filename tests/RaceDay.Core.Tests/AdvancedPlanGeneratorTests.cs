namespace RaceDay.Core.Tests;

using RaceDay.Core.Models;
using RaceDay.Core.Services;
using Xunit;

/// <summary>
/// Tests for the advanced nutrition planner with phase tracking, caffeine management,
/// and triathlon-specific rules
/// </summary>
public class AdvancedPlanGeneratorTests
{
    #region Test Helpers

    private Product CreateGel(string name = "Energy Gel", double carbs = 25, bool hasCaffeine = false, double caffeineMg = 0, string texture = "Gel")
    {
        return new Product(name, "gel", carbs, 100, 0, hasCaffeine, caffeineMg, texture, "Energy");
    }

    private Product CreateDrink(string name = "Electrolyte Drink", double carbs = 30, double volumeMl = 500, string texture = "Drink", string type = "Electrolyte")
    {
        return new Product(name, "drink", carbs, 300, volumeMl, false, 0, texture, type);
    }

    private Product CreateLightGel(string name = "Light Gel", double carbs = 20)
    {
        return new Product(name, "gel", carbs, 80, 0, false, 0, "LightGel", "Energy");
    }

    private Product CreateBetaFuelGel(string name = "Beta Fuel Gel", double carbs = 40, bool hasCaffeine = false, double caffeineMg = 0)
    {
        return new Product(name, "gel", carbs, 120, 0, hasCaffeine, caffeineMg, "Gel", "Energy");
    }

    private Product CreateBake(string name = "Energy Bake", double carbs = 35)
    {
        return new Product(name, "bar", carbs, 120, 0, false, 0, "Bake", "Energy");
    }

    #endregion

    #region 1. General Plan Structure - Phase Validation

    [Fact]
    public void Advanced_Plan_Events_ShouldBeSortedByTime()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(),
            CreateDrink()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Running, 2.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        var times = plan.Schedule.Select(s => s.TimeMin).ToList();
        var sortedTimes = times.OrderBy(t => t).ToList();
        Assert.Equal(sortedTimes, times);
    }

    [Fact]
    public void Triathlon_Plan_ShouldNotContainSwimIntakeDuringRace()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(),
            CreateDrink(),
            CreateLightGel()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.TriathlonHalf, 5.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        // During race (TimeMin >= 0), there should be no events with Phase == RacePhase.Swim
        var swimIntakes = plan.Schedule.Where(i => i.TimeMin >= 0 && i.Phase == RacePhase.Swim).ToList();
        Assert.Empty(swimIntakes);
    }

    [Fact]
    public void Running_Plan_AllEvents_ShouldHaveRunPhase()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> { CreateGel() };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Running, 2.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        Assert.All(plan.Schedule.Where(i => i.TimeMin >= 0), 
            item => Assert.Equal(RacePhase.Run, item.Phase));
    }

    [Fact]
    public void Cycling_Plan_AllEvents_ShouldHaveBikePhase()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(),
            CreateDrink()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Cycling, 3.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        Assert.All(plan.Schedule.Where(i => i.TimeMin >= 0), 
            item => Assert.Equal(RacePhase.Bike, item.Phase));
    }

    [Fact]
    public void Triathlon_Plan_ShouldNotHaveRunBeforeBike()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(),
            CreateDrink(),
            CreateLightGel(),
            CreateBake()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.TriathlonFull, 11.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        var bikeEvents = plan.Schedule.Where(i => i.Phase == RacePhase.Bike).ToList();
        var runEvents = plan.Schedule.Where(i => i.Phase == RacePhase.Run).ToList();
        
        if (bikeEvents.Any() && runEvents.Any())
        {
            var lastBikeTime = bikeEvents.Max(e => e.TimeMin);
            var firstRunTime = runEvents.Min(e => e.TimeMin);
            Assert.True(firstRunTime >= lastBikeTime, 
                $"Run phase should not start before bike phase ends. First run: {firstRunTime}, Last bike: {lastBikeTime}");
        }
    }

    #endregion

    #region 3. Caffeine Behaviour

    [Theory]
    [InlineData(RaceMode.Running, 1.0)]
    [InlineData(RaceMode.Cycling, 1.5)]
    [InlineData(RaceMode.TriathlonHalf, 2.0)]
    [InlineData(RaceMode.TriathlonFull, 3.0)]
    public void Caffeine_ShouldNotAppearTooEarly(RaceMode mode, double startCaffeineHour)
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel("Regular Gel", carbs: 25, hasCaffeine: false),
            CreateGel("Caffeine Gel", carbs: 25, hasCaffeine: true, caffeineMg: 100),
            CreateDrink()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            mode, 4.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        var startCaffeineMin = startCaffeineHour * 60;
        var earlyCaffeineIntakes = plan.Schedule
            .Where(i => i.TimeMin < startCaffeineMin && i.Product?.HasCaffeine == true)
            .ToList();
        
        Assert.Empty(earlyCaffeineIntakes);
    }

    [Fact]
    public void Caffeine_TotalDose_ShouldNotExceedLimit()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var maxCaffeineMg = NutritionConfig.MaxCaffeineMgPerKg * athlete.WeightKg;
        var products = new List<Product> 
        { 
            CreateGel("Regular Gel", carbs: 25, hasCaffeine: false),
            CreateGel("Caffeine Gel", carbs: 25, hasCaffeine: true, caffeineMg: 100),
            CreateDrink()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Running, 4.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 15);

        // Assert
        var totalCaffeineMg = plan.Schedule
            .Where(i => i.Product?.HasCaffeine == true)
            .Sum(i => i.AmountPortions * i.Product!.CaffeineMg);
        
        Assert.True(totalCaffeineMg <= maxCaffeineMg,
            $"Total caffeine ({totalCaffeineMg}mg) exceeds limit ({maxCaffeineMg}mg)");
    }

    [Fact]
    public void Caffeine_MinimumSpacing_BetweenCaffeinatedEvents()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel("Regular Gel", carbs: 25, hasCaffeine: false),
            CreateGel("Caffeine Gel", carbs: 25, hasCaffeine: true, caffeineMg: 80),
            CreateDrink()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Running, 4.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        var caffeinatedEvents = plan.Schedule
            .Where(i => i.Product?.HasCaffeine == true)
            .OrderBy(i => i.TimeMin)
            .ToList();

        for (int i = 1; i < caffeinatedEvents.Count; i++)
        {
            var gap = caffeinatedEvents[i].TimeMin - caffeinatedEvents[i - 1].TimeMin;
            Assert.True(gap >= NutritionConfig.MinCaffeineSpacingMin,
                $"Caffeine spacing between events at {caffeinatedEvents[i - 1].TimeMin} and {caffeinatedEvents[i].TimeMin} " +
                $"is {gap} minutes, less than minimum of {NutritionConfig.MinCaffeineSpacingMin} minutes");
        }
    }

    [Fact]
    public void Caffeine_MostIntake_ShouldBeInSecondHalfOfRace()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var durationHours = 4.0;
        var products = new List<Product> 
        { 
            CreateGel("Regular Gel", carbs: 25, hasCaffeine: false),
            CreateGel("Caffeine Gel", carbs: 25, hasCaffeine: true, caffeineMg: 80),
            CreateDrink()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Running, durationHours, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        var halfwayMin = 0.5 * durationHours * 60;
        var caffeinatedEvents = plan.Schedule
            .Where(i => i.Product?.HasCaffeine == true)
            .ToList();

        if (caffeinatedEvents.Any())
        {
            var secondHalfCount = caffeinatedEvents.Count(i => i.TimeMin >= halfwayMin);
            var firstHalfCount = caffeinatedEvents.Count(i => i.TimeMin < halfwayMin);
            
            Assert.True(secondHalfCount >= firstHalfCount,
                $"Most caffeine should be in second half. First half: {firstHalfCount}, Second half: {secondHalfCount}");
        }
    }

    #endregion

    #region 4. Triathlon-Specific Logic

    [Fact]
    public void Triathlon_FirstThirtyMinutesBike_ShouldOnlyHaveElectrolyteDrinks()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(),
            CreateDrink("Electrolyte Drink", carbs: 30, volumeMl: 100, texture: "Drink", type: "Electrolyte"),
            CreateDrink("Energy Drink", carbs: 40, volumeMl: 100, texture: "Drink", type: "Energy"),
            CreateLightGel(),
            CreateBake()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.TriathlonHalf, 5.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 10);

        // Assert
        // Find bike start time (after swim, which is ~40 min for half triathlon)
        var bikeEvents = plan.Schedule.Where(i => i.Phase == RacePhase.Bike).ToList();
        
        if (bikeEvents.Any())
        {
            var bikeStartMin = bikeEvents.Min(e => e.TimeMin);
            var firstThirtyMinBike = bikeEvents
                .Where(e => e.TimeMin < bikeStartMin + 30)
                .ToList();

            Assert.All(firstThirtyMinBike, item =>
            {
                Assert.NotNull(item.Product);
                Assert.Equal("Drink", item.Product!.Texture);
                Assert.Equal("Electrolyte", item.Product!.Type);
            });
        }
    }

    [Fact]
    public void Triathlon_FirstHourRun_ShouldOnlyHaveLightTextures()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(),
            CreateDrink("Electrolyte Drink", carbs: 30, volumeMl: 100, texture: "Drink", type: "Electrolyte"),
            CreateLightGel("Light Gel"),
            CreateBake("Energy Bake")
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.TriathlonHalf, 5.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 15);

        // Assert
        var runEvents = plan.Schedule.Where(i => i.Phase == RacePhase.Run).ToList();
        
        if (runEvents.Any())
        {
            var runStartMin = runEvents.Min(e => e.TimeMin);
            var firstHourRun = runEvents
                .Where(e => e.TimeMin < runStartMin + 60)
                .ToList();

            Assert.All(firstHourRun, item =>
            {
                Assert.NotNull(item.Product);
                Assert.True(
                    item.Product!.Texture == "LightGel" || item.Product!.Texture == "Bake",
                    $"Expected LightGel or Bake, got {item.Product!.Texture}");
            });
        }
    }

    [Fact]
    public void Race_EndPhase_ShouldIncludeBetaFuelGel()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var durationHours = 3.0;
        var products = new List<Product> 
        { 
            CreateGel("Regular Gel"),
            CreateBetaFuelGel("Beta Fuel Gel"),
            CreateDrink()
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Running, durationHours, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        var endPhaseMin = 0.8 * durationHours * 60;
        var endPhaseEvents = plan.Schedule
            .Where(i => i.TimeMin >= endPhaseMin && i.Product != null)
            .ToList();

        var betaFuelGels = endPhaseEvents
            .Where(i => i.Product!.Texture == "Gel" && 
                       i.Product!.Name.Contains("Beta Fuel", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(betaFuelGels);
    }

    #endregion

    #region 5. Data Consistency

    [Fact]
    public void Plan_TotalCarbsSoFar_ShouldBeConsistent()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(carbs: 25),
            CreateDrink(carbs: 30)
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Cycling, 2.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        var positiveTimeEvents = plan.Schedule.Where(i => i.TimeMin >= 0 && i.Product != null).ToList();
        var sumCarbs = positiveTimeEvents.Sum(i => i.AmountPortions * i.Product!.CarbsG);
        
        if (plan.Schedule.Any())
        {
            var lastEvent = plan.Schedule.Last();
            Assert.Equal(sumCarbs, lastEvent.TotalCarbsSoFar);
        }
    }

    [Fact]
    public void Advanced_Plan_ProductSummary_ShouldMatchSchedule()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(carbs: 25),
            CreateDrink(carbs: 30)
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            RaceMode.Cycling, 2.0, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        foreach (var summary in plan.ProductSummaries)
        {
            var scheduleSum = plan.Schedule
                .Where(item => item.ProductName == summary.ProductName && item.TimeMin >= 0)
                .Sum(item => item.AmountPortions);
            
            Assert.Equal(scheduleSum, summary.TotalPortions);
        }
    }

    #endregion

    #region Integration Tests

    [Theory]
    [InlineData(RaceMode.Running, 2.0)]
    [InlineData(RaceMode.Cycling, 3.0)]
    [InlineData(RaceMode.TriathlonHalf, 5.0)]
    [InlineData(RaceMode.TriathlonFull, 10.0)]
    public void Advanced_Plan_ForAllRaceModes_ShouldSucceed(RaceMode mode, double durationHours)
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var products = new List<Product> 
        { 
            CreateGel(),
            CreateDrink(),
            CreateLightGel(),
            CreateBake(),
            CreateBetaFuelGel(),
            CreateGel("Caffeine Gel", carbs: 25, hasCaffeine: true, caffeineMg: 100)
        };

        // Act
        var plan = AdvancedPlanGenerator.GenerateAdvanced(
            mode, durationHours, TemperatureCondition.Moderate, IntensityLevel.Moderate,
            athlete, products, intervalMin: 20);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan.Schedule);
        Assert.True(plan.TotalCarbsG > 0);
        
        // Verify all events have phase information
        Assert.All(plan.Schedule.Where(i => i.TimeMin >= 0), 
            item => Assert.NotNull(item.Phase));
    }

    #endregion
}
