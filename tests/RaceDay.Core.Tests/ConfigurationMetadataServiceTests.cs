namespace RaceDay.Core.Tests;

using RaceDay.Core.Services;
using RaceDay.Core.Models;

public class ConfigurationMetadataServiceTests
{
    [Fact]
    public void GetConfigurationMetadata_ReturnsValidConfig()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Phases);
        Assert.NotNull(config.Sports);
        Assert.NotNull(config.TemperatureAdjustments);
        Assert.NotNull(config.AthleteWeightThresholds);
        Assert.NotNull(config.NutritionTargets);
    }

    [Fact]
    public void GetConfigurationMetadata_ContainsAllPhases()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.NotNull(config.Phases);
        Assert.NotEmpty(config.Phases);
        // Should have phases for each race type
        Assert.True(config.Phases.Count >= 3);
    }

    [Fact]
    public void GetConfigurationMetadata_ContainsSwimPhase()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var swimPhase = config.Phases.FirstOrDefault(p => p.Phase == RacePhase.Swim);

        // Assert
        Assert.NotNull(swimPhase);
        Assert.NotEmpty(swimPhase.Name);
        Assert.NotEmpty(swimPhase.Description);
    }

    [Fact]
    public void GetConfigurationMetadata_ContainsAllSportTypes()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.NotNull(config.Sports);
        Assert.Equal(3, config.Sports.Count);
    }

    [Fact]
    public void GetConfigurationMetadata_RunSportHasValidConfig()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var runSport = config.Sports.FirstOrDefault(s => s.SportType == "Run");

        // Assert
        Assert.NotNull(runSport);
        Assert.Equal("Running", runSport.Name);
        Assert.NotEmpty(runSport.Description);
        Assert.True(runSport.CarbsPerKgPerHour > 0);
        Assert.True(runSport.MaxCarbsPerHour > 0);
        Assert.True(runSport.SlotIntervalMinutes > 0);
        Assert.True(runSport.CaffeineStartHour >= 0);
        Assert.True(runSport.CaffeineIntervalHours > 0);
        Assert.True(runSport.MaxCaffeineMgPerKg > 0);
    }

    [Fact]
    public void GetConfigurationMetadata_BikeSportHasValidConfig()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var bikeSport = config.Sports.FirstOrDefault(s => s.SportType == "Bike");

        // Assert
        Assert.NotNull(bikeSport);
        Assert.Equal("Cycling", bikeSport.Name);
        Assert.NotEmpty(bikeSport.Description);
        Assert.True(bikeSport.CarbsPerKgPerHour > 0);
        Assert.True(bikeSport.MaxCarbsPerHour > 0);
    }

    [Fact]
    public void GetConfigurationMetadata_TriathlonSportHasValidConfig()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var triSport = config.Sports.FirstOrDefault(s => s.SportType == "Triathlon");

        // Assert
        Assert.NotNull(triSport);
        Assert.Equal("Triathlon", triSport.Name);
        Assert.NotEmpty(triSport.Description);
    }

    [Fact]
    public void GetConfigurationMetadata_TemperatureAdjustmentsExist()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.NotNull(config.TemperatureAdjustments);
        Assert.Equal(3, config.TemperatureAdjustments.Count);
    }

    [Fact]
    public void GetConfigurationMetadata_ColdTemperatureHasAdjustment()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var coldAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Cold");

        // Assert
        Assert.NotNull(coldAdj);
        Assert.NotEmpty(coldAdj.Range);
        Assert.NotEmpty(coldAdj.Description);
        Assert.True(coldAdj.FluidBonus <= 0);
    }

    [Fact]
    public void GetConfigurationMetadata_ModerateTemperatureHasAdjustment()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var modAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Moderate");

        // Assert
        Assert.NotNull(modAdj);
        Assert.NotEmpty(modAdj.Range);
        Assert.NotEmpty(modAdj.Description);
        Assert.Equal(0, modAdj.FluidBonus);
    }

    [Fact]
    public void GetConfigurationMetadata_HotTemperatureHasAdjustment()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var hotAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Hot");

        // Assert
        Assert.NotNull(hotAdj);
        Assert.NotEmpty(hotAdj.Range);
        Assert.NotEmpty(hotAdj.Description);
        Assert.True(hotAdj.FluidBonus > 0);
    }

    [Fact]
    public void GetConfigurationMetadata_AthleteWeightThresholdsExist()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.NotNull(config.AthleteWeightThresholds);
        Assert.NotEmpty(config.AthleteWeightThresholds);
    }

    [Fact]
    public void GetConfigurationMetadata_AthleteWeightThresholdsHaveValidValues()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.All(config.AthleteWeightThresholds, threshold =>
        {
            Assert.True(threshold.ThresholdKg > 0);
            Assert.NotEmpty(threshold.Category);
            Assert.NotEmpty(threshold.Description);
        });
    }

    [Fact]
    public void GetConfigurationMetadata_NutritionTargetsExist()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.NotNull(config.NutritionTargets);
        Assert.NotEmpty(config.NutritionTargets);
    }

    [Fact]
    public void GetConfigurationMetadata_NutritionTargetsHaveValidRanges()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.All(config.NutritionTargets, target =>
        {
            Assert.NotEmpty(target.Name);
            Assert.NotEmpty(target.Unit);
            Assert.NotEmpty(target.Description);
            Assert.True(target.MaxValue >= target.MinValue);
        });
    }

    [Fact]
    public void GetConfigurationMetadata_RunHasDifferentCaffeineStartThanBike()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var runSport = config.Sports.FirstOrDefault(s => s.SportType == "Run");
        var bikeSport = config.Sports.FirstOrDefault(s => s.SportType == "Bike");

        // Assert
        Assert.NotNull(runSport);
        Assert.NotNull(bikeSport);
        Assert.NotEqual(runSport.CaffeineStartHour, bikeSport.CaffeineStartHour);
    }

    [Fact]
    public void GetConfigurationMetadata_AllSportsCaffeineConfigValid()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.All(config.Sports, sport =>
        {
            Assert.True(sport.CaffeineStartHour >= 0);
            Assert.True(sport.CaffeineStartHour < 24);
            Assert.True(sport.CaffeineIntervalHours > 0);
            Assert.True(sport.MaxCaffeineMgPerKg > 0);
        });
    }

    [Fact]
    public void GetConfigurationMetadata_AllPhasesHaveValidDescriptions()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.All(config.Phases, phase =>
        {
            Assert.NotEmpty(phase.Name);
            Assert.NotEmpty(phase.Description);
        });
    }

    [Fact]
    public void GetConfigurationMetadata_MultipleCallsReturnConsistentResults()
    {
        // Act
        var config1 = ConfigurationMetadataService.GetConfigurationMetadata();
        var config2 = ConfigurationMetadataService.GetConfigurationMetadata();

        // Assert
        Assert.Equal(config1.Phases.Count, config2.Phases.Count);
        Assert.Equal(config1.Sports.Count, config2.Sports.Count);
        Assert.Equal(config1.TemperatureAdjustments.Count, config2.TemperatureAdjustments.Count);
    }

    [Fact]
    public void GetConfigurationMetadata_BikeCarbsPerKgHigherThanRun()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var runSport = config.Sports.FirstOrDefault(s => s.SportType == "Run");
        var bikeSport = config.Sports.FirstOrDefault(s => s.SportType == "Bike");

        // Assert
        Assert.NotNull(runSport);
        Assert.NotNull(bikeSport);
        // Cycling requires more carbs per kg than running due to better digestion
        Assert.True(bikeSport.CarbsPerKgPerHour > runSport.CarbsPerKgPerHour);
    }

    [Fact]
    public void GetConfigurationMetadata_TemperatureBonusesAreLogical()
    {
        // Act
        var config = ConfigurationMetadataService.GetConfigurationMetadata();
        var coldAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Cold");
        var modAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Moderate");
        var hotAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Hot");

        // Assert - Temperature adjustments should increase with heat
        Assert.NotNull(coldAdj);
        Assert.NotNull(modAdj);
        Assert.NotNull(hotAdj);
        Assert.True(coldAdj.FluidBonus < modAdj.FluidBonus);
        Assert.True(modAdj.FluidBonus < hotAdj.FluidBonus);
    }
}
