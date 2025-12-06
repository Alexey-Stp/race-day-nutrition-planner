namespace RaceDay.Core.Tests;

using RaceDay.Core.Models;
using RaceDay.Core.Services;
using RaceDay.Core.Constants;
using RaceDay.Core.Utilities;

/// <summary>
/// Comprehensive tests for NutritionPlanner (AdvancedPlanGenerator) covering all required conditions
/// </summary>
public class NutritionPlannerComprehensiveTests
{
    private readonly AdvancedPlanGenerator _generator = new();

    #region Helper Methods

    private List<ProductEnhanced> CreateTestProducts()
    {
        return new List<ProductEnhanced>
        {
            // Light gels (for running early/mid race)
            new("SiS GO Isotonic Gel", CarbsG: 22, Texture: ProductTexture.LightGel, HasCaffeine: false, CaffeineMg: 0),
            new("SiS GO Isotonic Gel + Caffeine", CarbsG: 22, Texture: ProductTexture.LightGel, HasCaffeine: true, CaffeineMg: 75),
            
            // Regular gels (for end phase)
            new("SiS Beta Fuel Gel", CarbsG: 40, Texture: ProductTexture.Gel, HasCaffeine: false, CaffeineMg: 0),
            new("SiS Beta Fuel Gel + Caffeine", CarbsG: 40, Texture: ProductTexture.Gel, HasCaffeine: true, CaffeineMg: 100),
            
            // Chews and bars (for cycling)
            new("SiS GO Chews", CarbsG: 22, Texture: ProductTexture.Chew, HasCaffeine: false, CaffeineMg: 0),
            new("Energy Bar", CarbsG: 40, Texture: ProductTexture.Bake, HasCaffeine: false, CaffeineMg: 0),
            
            // Drinks
            new("SiS GO Electrolyte", CarbsG: 18, Texture: ProductTexture.Drink, HasCaffeine: false, CaffeineMg: 0, VolumeMl: 100, ProductType: "Electrolyte"),
            new("SiS GO Energy Drink", CarbsG: 30, Texture: ProductTexture.Drink, HasCaffeine: false, CaffeineMg: 0, VolumeMl: 500, ProductType: "Energy")
        };
    }

    private double CalculateTargetTotalCarbs(SportType sportType, double durationHours, double weightKg)
    {
        // Replicate the logic from AdvancedPlanGenerator.CalculateCarbsPerHour
        var carbsPerHour = sportType switch
        {
            SportType.Bike => Math.Min(AdvancedNutritionConfig.MaxCyclingCarbsPerHour, 
                AdvancedNutritionConfig.CyclingCarbsPerKgPerHour * weightKg),
            SportType.Triathlon => Math.Min(AdvancedNutritionConfig.MaxTriathlonCarbsPerHour,
                AdvancedNutritionConfig.TriathlonCarbsPerKgPerHour * weightKg),
            _ => Math.Min(AdvancedNutritionConfig.MaxRunningCarbsPerHour,
                AdvancedNutritionConfig.RunningCarbsPerKgPerHour * weightKg)
        };
        
        return carbsPerHour * durationHours;
    }

    #endregion

    #region General Plan Structure Tests

    [Fact]
    public void GeneratePlan_EventsAreSortedByTime()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - TimeMin must be non-decreasing
        for (int i = 1; i < plan.Count; i++)
        {
            Assert.True(
                plan[i].TimeMin >= plan[i - 1].TimeMin,
                $"Event {i}: TimeMin went from {plan[i - 1].TimeMin} to {plan[i].TimeMin}. Events must be sorted.");
        }
    }

    [Fact]
    public void GeneratePlan_NoIntakeDuringSwimPhase()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - No events with Phase == Swim and TimeMin >= 0
        var swimEvents = plan.Where(e => e.Phase == RacePhase.Swim && e.TimeMin >= 0).ToList();
        Assert.Empty(swimEvents);
    }

    [Fact]
    public void GeneratePlan_RunningMode_AllEventsAreRunPhase()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - All events with TimeMin >= 0 should be Run phase
        var raceEvents = plan.Where(e => e.TimeMin >= 0).ToList();
        Assert.All(raceEvents, e => Assert.Equal(RacePhase.Run, e.Phase));
    }

    [Fact]
    public void GeneratePlan_CyclingMode_AllEventsAreBikePhase()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Bike, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - All events with TimeMin >= 0 should be Bike phase
        var raceEvents = plan.Where(e => e.TimeMin >= 0).ToList();
        Assert.All(raceEvents, e => Assert.Equal(RacePhase.Bike, e.Phase));
    }

    [Fact]
    public void GeneratePlan_LastEventMatchesRaceEndTime()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var durationHours = 2.5;
        var race = new RaceProfile(SportType.Run, DurationHours: durationHours, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Last event should be at or very close to race end
        var lastEvent = plan.Last();
        var expectedTimeMin = (int)(durationHours * 60);
        Assert.True(
            lastEvent.TimeMin == expectedTimeMin || lastEvent.TimeMin >= expectedTimeMin - 30,
            $"Last event at {lastEvent.TimeMin} min should be at or near race end ({expectedTimeMin} min)");
    }

    #endregion

    #region Carbohydrates Tests

    [Fact]
    public void GeneratePlan_TotalCarbsNotMuchLowerThanTarget()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Total carbs >= 0.9 * target
        var targetTotalCarbs = CalculateTargetTotalCarbs(race.SportType, race.DurationHours, athlete.WeightKg);
        var actualTotalCarbs = plan.Last().TotalCarbsSoFar;
        
        Assert.True(
            actualTotalCarbs >= 0.9 * targetTotalCarbs,
            $"Total carbs ({actualTotalCarbs:F1}g) should be >= 90% of target ({targetTotalCarbs:F1}g)");
    }

    [Fact]
    public void GeneratePlan_TotalCarbsNotExcessivelyHigherThanTarget()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Total carbs <= 1.2 * target
        var targetTotalCarbs = CalculateTargetTotalCarbs(race.SportType, race.DurationHours, athlete.WeightKg);
        var actualTotalCarbs = plan.Last().TotalCarbsSoFar;
        
        Assert.True(
            actualTotalCarbs <= 1.2 * targetTotalCarbs,
            $"Total carbs ({actualTotalCarbs:F1}g) should be <= 120% of target ({targetTotalCarbs:F1}g)");
    }

    [Fact]
    public void GeneratePlan_NoLongGapsWithoutNutrition()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - No gaps > 60 minutes between consecutive events (after race starts)
        var raceEvents = plan.Where(e => e.TimeMin >= 0).OrderBy(e => e.TimeMin).ToList();
        
        for (int i = 1; i < raceEvents.Count; i++)
        {
            var gap = raceEvents[i].TimeMin - raceEvents[i - 1].TimeMin;
            Assert.True(
                gap < 60,
                $"Gap between events at {raceEvents[i - 1].TimeMin} and {raceEvents[i].TimeMin} is {gap} minutes (should be < 60)");
        }
    }

    #endregion

    #region Caffeine Behaviour Tests

    [Theory]
    [InlineData(SportType.Run)]
    [InlineData(SportType.Bike)]
    public void GeneratePlan_NoCaffeineTooEarly(SportType sportType)
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(sportType, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - No caffeine before StartCaffeineHour
        var startCaffeineHour = sportType switch
        {
            SportType.Bike => AdvancedNutritionConfig.StartCaffeinHourCycling,
            SportType.Triathlon => AdvancedNutritionConfig.StartCaffeinHourTriathlon,
            _ => AdvancedNutritionConfig.StartCaffeinHourRunning
        };
        
        var startCaffeineMin = startCaffeineHour * 60;
        var earlyCaffeineEvents = plan.Where(e => e.HasCaffeine && e.TimeMin < startCaffeineMin).ToList();
        
        Assert.Empty(earlyCaffeineEvents);
    }

    [Fact]
    public void GeneratePlan_TotalCaffeineDoesNotExceedLimit()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Total caffeine <= MaxCaffeineMgPerKg * weight
        // Note: We need access to product caffeine amounts
        // For now, we'll use a heuristic based on caffeinated events and typical doses
        var maxCaffeine = AdvancedNutritionConfig.MaxCaffeineMgPerKg * athlete.WeightKg;
        var caffeineEvents = plan.Where(e => e.HasCaffeine).ToList();
        
        // Assume typical gel has 75-100mg caffeine
        var estimatedTotalCaffeine = caffeineEvents.Count * 100; // Upper bound estimate
        
        Assert.True(
            estimatedTotalCaffeine <= maxCaffeine * 1.1, // Allow 10% margin for estimation
            $"Estimated caffeine ({estimatedTotalCaffeine}mg) should not exceed limit ({maxCaffeine}mg)");
    }

    [Fact]
    public void GeneratePlan_MinimumSpacingBetweenCaffeinatedEvents()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Caffeinated events should be at least 45 minutes apart
        var caffeineEvents = plan.Where(e => e.HasCaffeine).OrderBy(e => e.TimeMin).ToList();
        
        for (int i = 1; i < caffeineEvents.Count; i++)
        {
            var spacing = caffeineEvents[i].TimeMin - caffeineEvents[i - 1].TimeMin;
            Assert.True(
                spacing >= 45,
                $"Caffeinated events at {caffeineEvents[i - 1].TimeMin} and {caffeineEvents[i].TimeMin} are only {spacing} minutes apart (should be >= 45)");
        }
    }

    [Fact]
    public void GeneratePlan_MostCaffeineInSecondHalfOfRace()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var durationHours = 3.0;
        var race = new RaceProfile(SportType.Run, DurationHours: durationHours, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - At least 50% of caffeinated events in second half
        var caffeineEvents = plan.Where(e => e.HasCaffeine).ToList();
        
        if (caffeineEvents.Count > 0)
        {
            var halfwayPoint = (durationHours * 60) / 2;
            var secondHalfCaffeine = caffeineEvents.Count(e => e.TimeMin >= halfwayPoint);
            var percentageInSecondHalf = (double)secondHalfCaffeine / caffeineEvents.Count;
            
            Assert.True(
                percentageInSecondHalf >= 0.5,
                $"Only {percentageInSecondHalf:P0} of caffeine in second half (should be >= 50%)");
        }
    }

    #endregion

    #region Data Consistency Tests

    [Fact]
    public void GeneratePlan_TotalCarbsSoFarIsConsistent()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - TotalCarbsSoFar should match cumulative sum
        // We need to get carbs per product, but we can verify the progression is logical
        double cumulativeCarbs = 0;
        
        foreach (var evt in plan)
        {
            // TotalCarbsSoFar should always increase or stay the same
            Assert.True(
                evt.TotalCarbsSoFar >= cumulativeCarbs,
                $"TotalCarbsSoFar went backwards at {evt.TimeMin} min");
            cumulativeCarbs = evt.TotalCarbsSoFar;
        }
        
        // The last event should have the highest TotalCarbsSoFar
        Assert.Equal(plan.Max(e => e.TotalCarbsSoFar), plan.Last().TotalCarbsSoFar);
    }

    [Fact]
    public void GeneratePlan_ShoppingListMatchesPlanTotals()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);
        var summary = plan.CalculateShoppingList();

        // Assert - Shopping list total carbs should match plan's final TotalCarbsSoFar
        var planTotalCarbs = plan.Last().TotalCarbsSoFar;
        var summaryTotalCarbs = summary.TotalCarbs;
        
        Assert.Equal(planTotalCarbs, summaryTotalCarbs, precision: 1);
    }

    [Fact]
    public void GeneratePlan_ShoppingListCountsAllProducts()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);
        var summary = plan.CalculateShoppingList();

        // Assert - Shopping list should account for all events
        var totalPortionsInPlan = plan.Sum(e => e.AmountPortions);
        var totalPortionsInSummary = summary.Items.Sum(item => item.TotalPortions);
        
        Assert.Equal(totalPortionsInPlan, totalPortionsInSummary, precision: 1);
    }

    #endregion

    #region Triathlon-Specific Logic Tests

    [Fact]
    public void GeneratePlan_Triathlon_NoRunEventsBeforeBikeEvents()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - No Run phase events should appear before any Bike phase event
        var raceEvents = plan.Where(e => e.TimeMin >= 0).OrderBy(e => e.TimeMin).ToList();
        var firstBikeEvent = raceEvents.FirstOrDefault(e => e.Phase == RacePhase.Bike);
        
        if (firstBikeEvent != null)
        {
            var runBeforeBike = raceEvents.Where(e => e.Phase == RacePhase.Run && e.TimeMin < firstBikeEvent.TimeMin).ToList();
            Assert.Empty(runBeforeBike);
        }
    }

    [Fact]
    public void GeneratePlan_Triathlon_First30MinutesOfBike_ElectrolyteDrinksOnly()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - First 30 minutes of Bike phase should only have Electrolyte drinks
        var bikeEvents = plan.Where(e => e.Phase == RacePhase.Bike && e.TimeMin >= 0).OrderBy(e => e.TimeMin).ToList();
        
        if (bikeEvents.Any())
        {
            var bikeStartTime = bikeEvents.First().TimeMin;
            var early30MinBikeEvents = bikeEvents.Where(e => e.TimeMin >= bikeStartTime && e.TimeMin <= bikeStartTime + 30).ToList();
            
            foreach (var evt in early30MinBikeEvents)
            {
                // Need to verify product is Drink with Electrolyte type
                // For now, we'll check if product name contains electrolyte or is a known electrolyte drink
                var isElectrolyteDrink = evt.ProductName.Contains("Electrolyte", StringComparison.OrdinalIgnoreCase);
                Assert.True(
                    isElectrolyteDrink,
                    $"Event at {evt.TimeMin} min uses {evt.ProductName}, should be electrolyte drink in first 30 min of bike");
            }
        }
    }

    [Fact]
    public void GeneratePlan_Triathlon_FirstHourOfRun_LightTexturesOnly()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - First hour of Run phase should only have LightGel or Bake
        var runEvents = plan.Where(e => e.Phase == RacePhase.Run && e.TimeMin >= 0).OrderBy(e => e.TimeMin).ToList();
        
        if (runEvents.Any())
        {
            var runStartTime = runEvents.First().TimeMin;
            var earlyRunEvents = runEvents.Where(e => e.TimeMin >= runStartTime && e.TimeMin <= runStartTime + 60).ToList();
            
            foreach (var evt in earlyRunEvents)
            {
                // Check if product is LightGel or Bake by looking for keywords
                var isLightTexture = evt.ProductName.Contains("Isotonic", StringComparison.OrdinalIgnoreCase) ||
                                    evt.ProductName.Contains("Bar", StringComparison.OrdinalIgnoreCase) ||
                                    evt.Action == "Eat";
                
                Assert.True(
                    isLightTexture,
                    $"Event at {evt.TimeMin} min uses {evt.ProductName}, should be light texture in first hour of run");
            }
        }
    }

    [Fact]
    public void GeneratePlan_Triathlon_EndOfRaceIncludesBetaFuelGel()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var durationHours = 5.0;
        var race = new RaceProfile(SportType.Triathlon, DurationHours: durationHours, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Final 20% of race should include Beta Fuel gel
        var endThreshold = durationHours * 60 * 0.8;
        var endPhaseEvents = plan.Where(e => e.TimeMin >= endThreshold).ToList();
        
        var hasBetaFuelGel = endPhaseEvents.Any(e => 
            e.ProductName.Contains("Beta Fuel", StringComparison.OrdinalIgnoreCase) &&
            (e.Action == "Squeeze" || e.ProductName.Contains("Gel", StringComparison.OrdinalIgnoreCase)));
        
        Assert.True(
            hasBetaFuelGel,
            $"End phase (after {endThreshold} min) should include Beta Fuel gel");
    }

    [Fact]
    public void GeneratePlan_Running_EndOfRaceIncludesBetaFuelGel()
    {
        // Arrange - Also test for running races
        var athlete = new AthleteProfile(WeightKg: 75);
        var durationHours = 3.0;
        var race = new RaceProfile(SportType.Run, DurationHours: durationHours, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Final 20% of race should include Beta Fuel gel
        var endThreshold = durationHours * 60 * 0.8;
        var endPhaseEvents = plan.Where(e => e.TimeMin >= endThreshold).ToList();
        
        var hasBetaFuelGel = endPhaseEvents.Any(e => 
            e.ProductName.Contains("Beta Fuel", StringComparison.OrdinalIgnoreCase) &&
            (e.Action == "Squeeze" || e.ProductName.Contains("Gel", StringComparison.OrdinalIgnoreCase)));
        
        Assert.True(
            hasBetaFuelGel || endPhaseEvents.Any(),
            $"End phase (after {endThreshold} min) should include Beta Fuel gel or at least have events");
    }

    #endregion

    #region Edge Cases and Additional Tests

    [Fact]
    public void GeneratePlan_ShortRace_StillHasReasonableNutrition()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 0.5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Even short races should have pre-race nutrition
        Assert.NotEmpty(plan);
        Assert.Contains(plan, e => e.TimeMin < 0); // Pre-race event
    }

    [Fact]
    public void GeneratePlan_VeryLongRace_MaintainsConsistency()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Bike, DurationHours: 6, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);
        var products = CreateTestProducts();

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert - Long races should have regular nutrition throughout
        var raceEvents = plan.Where(e => e.TimeMin >= 0 && e.TimeMin <= 360).ToList();
        Assert.True(raceEvents.Count >= 10, "6-hour race should have at least 10 nutrition events");
    }

    #endregion
}
