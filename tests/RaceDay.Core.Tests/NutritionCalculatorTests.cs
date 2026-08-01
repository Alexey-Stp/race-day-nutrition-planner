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
        targets.CarbsGPerHour.ShouldBe(50);
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
        targets.CarbsGPerHour.ShouldBe(70);
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
        targets.CarbsGPerHour.ShouldBe(90);
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
        targets.CarbsGPerHour.ShouldBe(80); // 70 + 10 for long race
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
        targets.FluidsMlPerHour.ShouldBe(700); // 500 + 200 for hot weather
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
        targets.FluidsMlPerHour.ShouldBe(400); // 500 - 100 for cold weather
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
        targets.FluidsMlPerHour.ShouldBe(550); // 500 + 50 for heavy athlete
        targets.SodiumMgPerHour.ShouldBe(500); // 400 + 100 for heavy athlete
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
        targets.FluidsMlPerHour.ShouldBe(450); // 500 - 50 for light athlete
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
        targets.FluidsMlPerHour.ShouldBe(350);
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
        targets.SodiumMgPerHour.ShouldBe(700); // 400 + 200 (hot) + 100 (heavy)
    }

    [Fact]
    public void CalculateTargets_LongRaceEasyIntensity_NoLongRaceBonus()
    {
        // Easy intensity never gets the long-race bonus regardless of duration
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 6, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Easy);

        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        targets.CarbsGPerHour.ShouldBe(50); // no bonus for easy
    }

    // ── CalculateMultiNutrientTargets ────────────────────────────────────────

    [Fact]
    public void CalculateMultiNutrientTargets_CaffeineDisabled_ReturnsZeroCaffeine()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled: false);

        targets.CaffeineMg.ShouldBe(0);
    }

    [Fact]
    public void CalculateMultiNutrientTargets_CaffeineEnabled_EasyIntensity_Uses1MgPerKg()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Easy);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled: true);

        targets.CaffeineMg.ShouldBe(70 * 1.0); // 1 mg/kg for easy
    }

    [Fact]
    public void CalculateMultiNutrientTargets_CaffeineEnabled_ModerateIntensity_Uses3MgPerKg()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled: true);

        targets.CaffeineMg.ShouldBe(70 * 3.0); // 3 mg/kg for moderate
    }

    [Fact]
    public void CalculateMultiNutrientTargets_CaffeineEnabled_HardIntensity_Uses4MgPerKg()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled: true);

        targets.CaffeineMg.ShouldBe(70 * 4.0); // 4 mg/kg for hard
    }

    [Fact]
    public void CalculateMultiNutrientTargets_CaffeineEnabled_MaxIntensity_UsesHardCeiling()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        // Max intensity falls through to the default _ branch which uses HardMgPerKg
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: (IntensityLevel)999);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled: true);

        targets.CaffeineMg.ShouldBe(70 * 4.0); // default branch uses HardMgPerKg
    }

    [Fact]
    public void CalculateMultiNutrientTargets_CaffeineEnabled_HeavyAthlete_CapsAt300mg()
    {
        // 100 kg * 4 mg/kg = 400 mg → capped at 300
        var athlete = new AthleteProfile(WeightKg: 100);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled: true);

        targets.CaffeineMg.ShouldBe(300); // capped
    }

    [Fact]
    public void CalculateMultiNutrientTargets_TotalCarbsMatchDuration()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Run, DurationHours: 3, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete);

        targets.CarbsG.ShouldBe(targets.CarbsPerHour * 3, tolerance: 0.01);
        targets.SodiumMg.ShouldBe(targets.SodiumPerHour * 3, tolerance: 0.01);
        targets.FluidMl.ShouldBe(targets.FluidPerHour * 3, tolerance: 0.01);
    }

    [Fact]
    public void CalculateMultiNutrientTargets_NonTriathlon_NoSegmentTargets()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete);

        targets.SegmentTargets.ShouldBeNull();
    }

    [Fact]
    public void CalculateMultiNutrientTargets_Triathlon_ReturnsSegmentTargets()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete);

        targets.SegmentTargets.ShouldNotBeNull();
        targets.SegmentTargets!.ShouldContainKey(RacePhase.Swim);
        targets.SegmentTargets.ShouldContainKey(RacePhase.Bike);
        targets.SegmentTargets.ShouldContainKey(RacePhase.Run);
    }

    [Fact]
    public void CalculateMultiNutrientTargets_Triathlon_SwimHasZeroCarbs()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete);

        targets.SegmentTargets![RacePhase.Swim].CarbsG.ShouldBe(0);
    }

    [Fact]
    public void CalculateMultiNutrientTargets_Triathlon_BikeAndRunCarbRatiosSumTo1()
    {
        var athlete = new AthleteProfile(WeightKg: 70);
        var race = new RaceProfile(SportType.Triathlon, DurationHours: 5, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Hard);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete);

        var bikeCarbs = targets.SegmentTargets![RacePhase.Bike].CarbsG;
        var runCarbs = targets.SegmentTargets[RacePhase.Run].CarbsG;
        (bikeCarbs + runCarbs).ShouldBe(targets.CarbsG, tolerance: 0.01);
    }
}

