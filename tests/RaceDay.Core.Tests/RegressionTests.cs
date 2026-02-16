namespace RaceDay.Core.Tests;

using Xunit;
using RaceDay.Core.Services;
using RaceDay.Core.Models;
using System.Linq;

/// <summary>
/// Regression tests for specific reported bugs
/// </summary>
public class RegressionTests
{
    private readonly PlanGenerator _generator = new();

    [Fact]
    public void Bug_CaffeineAlwaysZero_WhenCaffeinatedProductsUsed()
    {
        // REGRESSION: Caffeine shows as 0mg even when caffeinated products are in schedule
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(
            SportType.Run,
            DurationHours: 3,
            Temperature: TemperatureCondition.Moderate,
            Intensity: IntensityLevel.Hard
        );

        var products = new List<ProductEnhanced>
        {
            new("Energy Bar", 40, ProductTexture.Bake, false, 0, 0, "", 100),
            new("Regular Gel", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Caffeine Gel", 30, ProductTexture.Gel, true, 75, 0, "", 60),  // Has 75mg caffeine
            new("Caffeine Gel 2", 28, ProductTexture.Gel, true, 100, 0, "", 55)  // Has 100mg caffeine
        };

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products, intervalMinutes: 22, caffeineEnabled: true);

        // Assert
        var caffeineEvents = plan.Where(e => e.HasCaffeine && e.CaffeineMg.HasValue && e.CaffeineMg > 0).ToList();

        // Should have at least one caffeine event
        Assert.NotEmpty(caffeineEvents);

        // Total caffeine from all events should be > 0
        var totalCaffeine = plan.Sum(e => e.CaffeineMg ?? 0);
        Assert.True(totalCaffeine > 0,
            $"Expected total caffeine > 0 when caffeinated products are used, got {totalCaffeine}mg");

        // TotalCaffeineSoFar should be correctly accumulated
        var lastEvent = plan.Last();
        Assert.True(lastEvent.TotalCaffeineSoFar > 0,
            $"Expected cumulative caffeine > 0 at end of plan, got {lastEvent.TotalCaffeineSoFar}mg");

        // The sum should match the last cumulative value
        Assert.Equal(totalCaffeine, lastEvent.TotalCaffeineSoFar, precision: 1);
    }

    [Fact]
    public void Bug_RunPlan4Hours_EndsEarlyAt2h30()
    {
        // REGRESSION: Run plan ~4h duration ends at ~2h30 instead of spanning full duration
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(
            SportType.Run,
            DurationHours: 4.0,  // 4 hours = 240 minutes
            Temperature: TemperatureCondition.Moderate,
            Intensity: IntensityLevel.Hard
        );

        var products = new List<ProductEnhanced>
        {
            new("Energy Bar", 40, ProductTexture.Bake, false, 0, 0, "", 100),
            new("Gel A", 30, ProductTexture.Gel, false, 0, 0, "", 60),
            new("Gel B", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Light Gel", 22, ProductTexture.LightGel, false, 0, 0, "", 45),
            new("Sports Drink", 35, ProductTexture.Drink, false, 0, 500, "Energy", 200)
        };

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert
        Assert.NotEmpty(plan);

        var lastEventTime = plan.Max(e => e.TimeMin);
        var raceDuration = race.DurationHours * 60;  // 240 minutes

        // Last event should be in the final 25% of the race (after 180 min / 3h)
        var minExpectedTime = raceDuration * 0.75;  // 180 minutes
        Assert.True(lastEventTime >= minExpectedTime,
            $"Expected last event after {minExpectedTime}min (75% of {raceDuration}min race), " +
            $"but last event was at {lastEventTime}min");

        // Should have events distributed throughout the race, not just front-loaded
        var eventsInFirstHalf = plan.Count(e => e.TimeMin < raceDuration / 2);
        var eventsInSecondHalf = plan.Count(e => e.TimeMin >= raceDuration / 2);

        Assert.True(eventsInSecondHalf > 0,
            "Expected some nutrition events in second half of race");

        // Should not be extremely front-loaded (>80% in first half would be bad)
        var firstHalfPercent = (double)eventsInFirstHalf / plan.Count;
        Assert.True(firstHalfPercent < 0.85,
            $"Plan is too front-loaded: {firstHalfPercent:P0} of events in first half");
    }

    [Fact]
    public void Bug_TriathlonPlan_MissingRunSegmentEvents()
    {
        // REGRESSION: Triathlon plan contains only Bike items, Run segment "drops out"
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(
            SportType.Triathlon,
            DurationHours: 4.5,  // Half Ironman duration
            Temperature: TemperatureCondition.Moderate,
            Intensity: IntensityLevel.Hard
        );

        var products = new List<ProductEnhanced>
        {
            new("Energy Bar", 40, ProductTexture.Bake, false, 0, 0, "", 100),
            new("Gel", 30, ProductTexture.Gel, false, 0, 0, "", 60),
            new("Isotonic Gel", 22, ProductTexture.Gel, false, 0, 0, "Isotonic", 60),
            new("Sports Drink", 35, ProductTexture.Drink, false, 0, 500, "Energy", 200),
            new("High Carb Drink", 45, ProductTexture.Drink, false, 0, 500, "Energy", 300)
        };

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products);

        // Assert
        var swimEvents = plan.Where(e => e.Phase == RacePhase.Swim).ToList();
        var bikeEvents = plan.Where(e => e.Phase == RacePhase.Bike).ToList();
        var runEvents = plan.Where(e => e.Phase == RacePhase.Run).ToList();

        // Must have events in Run segment
        Assert.NotEmpty(runEvents);
        Assert.True(runEvents.Count > 0,
            $"Expected Run phase events in triathlon, but found 0. " +
            $"Swim: {swimEvents.Count}, Bike: {bikeEvents.Count}, Run: {runEvents.Count}");

        // Run events should be in the correct time window
        // For 4.5h triathlon: Swim=0-0.9h (0-54min), Bike=0.9-3.15h (54-189min), Run=3.15-4.5h (189-270min)
        var expectedRunStartMin = 4.5 * 60 * 0.70;  // Run starts at 70% of race
        foreach (var runEvent in runEvents)
        {
            Assert.True(runEvent.TimeMin >= expectedRunStartMin - 10,  // Allow 10min margin for transitions
                $"Run event at {runEvent.TimeMin}min is before run segment starts at ~{expectedRunStartMin}min");
        }
    }

    [Fact]
    public void Bug_CaffeineDisabled_StillSchedulesCaffeinatedProducts()
    {
        // REGRESSION: When caffeine is disabled, caffeinated products still appear in schedule
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(
            SportType.Run,
            DurationHours: 2,
            Temperature: TemperatureCondition.Moderate,
            Intensity: IntensityLevel.Hard
        );

        var products = new List<ProductEnhanced>
        {
            new("Regular Gel", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Caffeine Gel", 30, ProductTexture.Gel, true, 75, 0, "", 60),
            new("Sports Drink", 35, ProductTexture.Drink, false, 0, 500, "Energy", 200)
        };

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products, caffeineEnabled: false);

        // Assert
        var caffeineEvents = plan.Where(e => e.HasCaffeine).ToList();
        var totalCaffeine = plan.Sum(e => e.CaffeineMg ?? 0);

        Assert.Empty(caffeineEvents);
        Assert.Equal(0, totalCaffeine);
    }

    [Fact]
    public void Bug_PlanTotals_DontMatchScheduleSum()
    {
        // REGRESSION: Printed totals (carbs, caffeine) don't match sum over schedule
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(
            SportType.Run,
            DurationHours: 3,
            Temperature: TemperatureCondition.Moderate,
            Intensity: IntensityLevel.Hard
        );

        var products = new List<ProductEnhanced>
        {
            new("Gel", 25, ProductTexture.Gel, false, 0, 0, "", 50),
            new("Caffeine Gel", 30, ProductTexture.Gel, true, 75, 0, "", 60),
            new("Sports Drink", 35, ProductTexture.Drink, false, 0, 500, "Energy", 200)
        };

        // Act
        var plan = _generator.GeneratePlan(race, athlete, products, caffeineEnabled: true);

        // Assert
        var sumCarbs = plan.Sum(e => e.CarbsInEvent);
        var sumCaffeine = plan.Sum(e => e.CaffeineMg ?? 0);

        var lastEvent = plan.Last();

        // Cumulative totals at end should equal sum of individual events
        Assert.Equal(sumCarbs, lastEvent.TotalCarbsSoFar, precision: 1);
        Assert.Equal(sumCaffeine, lastEvent.TotalCaffeineSoFar, precision: 1);
    }
}
