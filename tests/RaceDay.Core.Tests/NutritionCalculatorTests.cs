namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Services;

public class NutritionCalculatorTests
{
    [Fact]
    public void CalculateTargets_EasyIntensity_Returns50GCarbsPerHour()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Easy);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(50, targets.CarbsGPerHour);
    }

    [Fact]
    public void CalculateTargets_ModerateIntensity_Returns70GCarbsPerHour()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(70, targets.CarbsGPerHour);
    }

    [Fact]
    public void CalculateTargets_HardIntensity_Returns90GCarbsPerHour()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(90, targets.CarbsGPerHour);
    }

    [Fact]
    public void CalculateTargets_LongRaceModerateIntensity_AddsExtraCarbs()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 6, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(80, targets.CarbsGPerHour); // 70 + 10 for long race
    }

    [Fact]
    public void CalculateTargets_HotWeather_IncreasesFluidIntake()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Hot, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(700, targets.FluidsMlPerHour); // 500 + 200 for hot weather
    }

    [Fact]
    public void CalculateTargets_ColdWeather_DecreasesFluidIntake()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Cold, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(400, targets.FluidsMlPerHour); // 500 - 100 for cold weather
    }

    [Fact]
    public void CalculateTargets_HeavyAthlete_IncreasesFluidAndSodium()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 90);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(550, targets.FluidsMlPerHour); // 500 + 50 for heavy athlete
        Assert.Equal(500, targets.SodiumMgPerHour); // 400 + 100 for heavy athlete
    }

    [Fact]
    public void CalculateTargets_LightAthlete_DecreasesFluidIntake()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 55);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(450, targets.FluidsMlPerHour); // 500 - 50 for light athlete
    }

    [Fact]
    public void CalculateTargets_FluidsAreClamped_BetweenMinAndMax()
    {
        // Arrange - extreme cold + light athlete
        var athlete = new AthleteProfile(WeightKg: 50);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Cold, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert - 500 - 100 (cold) - 50 (light) = 350
        Assert.Equal(350, targets.FluidsMlPerHour);
    }

    [Fact]
    public void CalculateTargets_HotWeatherAndHeavyAthlete_IncreasesSodium()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 90);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Hot, Intensity: IntensityLevel.Moderate);

        // Act
        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        // Assert
        Assert.Equal(700, targets.SodiumMgPerHour); // 400 + 200 (hot) + 100 (heavy)
    }
}

