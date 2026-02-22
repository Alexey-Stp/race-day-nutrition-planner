namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Services;
using Xunit.Abstractions;

public class TriathlonPhaseTests
{
    private readonly ITestOutputHelper _output;

    public TriathlonPhaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DebugTriathlonPhases_HalfTriathlon()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 90);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 4.5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = new List<Product>
        {
            new Product("Maurten GEL 100", "gel", CarbsG: 25, SodiumMg: 85, VolumeMl: 40),
            new Product("Maurten Drink Mix 320 (500ml)", "drink", CarbsG: 80, SodiumMg: 345, VolumeMl: 500)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Debug output
        _output.WriteLine($"Total events: {plan.Count}");
        foreach (var evt in plan)
        {
            _output.WriteLine($"Time: {evt.TimeMin}min, Phase: {evt.Phase}, Product: {evt.ProductName}");
        }

        var swimEvents = plan.Where(e => e.Phase == RacePhase.Swim).ToList();
        var bikeEvents = plan.Where(e => e.Phase == RacePhase.Bike).ToList();
        var runEvents = plan.Where(e => e.Phase == RacePhase.Run).ToList();

        _output.WriteLine($"\nSwim events: {swimEvents.Count}");
        _output.WriteLine($"Bike events: {bikeEvents.Count}");
        _output.WriteLine($"Run events: {runEvents.Count}");

        // Assert - just to make test pass for now
        Assert.NotEmpty(plan);
    }

    [Fact]
    public void DebugTriathlonPhases_FullTriathlon()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 90);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 10, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);
        var products = new List<Product>
        {
            new Product("Maurten GEL 100", "gel", CarbsG: 25, SodiumMg: 85, VolumeMl: 40),
            new Product("Maurten Drink Mix 320 (500ml)", "drink", CarbsG: 80, SodiumMg: 345, VolumeMl: 500),
            new Product("Energy Bar", "bar", CarbsG: 35, SodiumMg: 150, VolumeMl: 0)
        };

        // Act
        var service = new NutritionPlanService();
        var plan = service.GeneratePlan(race, athlete, products);

        // Debug output
        _output.WriteLine($"Total events: {plan.Count}");
        foreach (var evt in plan)
        {
            _output.WriteLine($"Time: {evt.TimeMin}min, Phase: {evt.Phase}, Product: {evt.ProductName}");
        }

        var swimEvents = plan.Where(e => e.Phase == RacePhase.Swim).ToList();
        var bikeEvents = plan.Where(e => e.Phase == RacePhase.Bike).ToList();
        var runEvents = plan.Where(e => e.Phase == RacePhase.Run).ToList();

        _output.WriteLine($"\nSwim events: {swimEvents.Count}");
        _output.WriteLine($"Bike events: {bikeEvents.Count}");
        _output.WriteLine($"Run events: {runEvents.Count}");

        // Assert
        Assert.NotEmpty(plan);
    }
}
