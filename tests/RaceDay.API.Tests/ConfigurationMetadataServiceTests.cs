namespace RaceDay.API.Tests;

using RaceDay.API.Metadata;
using RaceDay.Core.Models;

public class ConfigurationMetadataServiceTests
{
    private readonly ConfigurationMetadataService _service = new();

    [Fact]
    public void GetConfigurationMetadata_ReturnsValidConfig()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        config.ShouldNotBeNull();
        config.Phases.ShouldNotBeNull();
        config.Sports.ShouldNotBeNull();
        config.TemperatureAdjustments.ShouldNotBeNull();
        config.AthleteWeightThresholds.ShouldNotBeNull();
        config.NutritionTargets.ShouldNotBeNull();
    }

    [Fact]
    public void GetConfigurationMetadata_ContainsAllPhases()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        config.Phases.ShouldNotBeNull();
        config.Phases.ShouldNotBeEmpty();
        // Should have phases for each race type
        (config.Phases.Count >= 3).ShouldBeTrue();
    }

    [Fact]
    public void GetConfigurationMetadata_ContainsSwimPhase()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var swimPhase = config.Phases.FirstOrDefault(p => p.Phase == RacePhase.Swim);

        // Assert
        swimPhase.ShouldNotBeNull();
        swimPhase.Name.ShouldNotBeEmpty();
        swimPhase.Description.ShouldNotBeEmpty();
    }

    [Fact]
    public void GetConfigurationMetadata_ContainsAllSportTypes()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        config.Sports.ShouldNotBeNull();
        config.Sports.Count.ShouldBe(3);
    }

    [Fact]
    public void GetConfigurationMetadata_RunSportHasValidConfig()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var runSport = config.Sports.FirstOrDefault(s => s.SportType == "Run");

        // Assert
        runSport.ShouldNotBeNull();
        runSport.Name.ShouldBe("Running");
        runSport.Description.ShouldNotBeEmpty();
        (runSport.CarbsPerKgPerHour > 0).ShouldBeTrue();
        (runSport.MaxCarbsPerHour > 0).ShouldBeTrue();
        (runSport.SlotIntervalMinutes > 0).ShouldBeTrue();
        (runSport.CaffeineStartHour >= 0).ShouldBeTrue();
        (runSport.CaffeineIntervalHours > 0).ShouldBeTrue();
        (runSport.MaxCaffeineMgPerKg > 0).ShouldBeTrue();
    }

    [Fact]
    public void GetConfigurationMetadata_BikeSportHasValidConfig()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var bikeSport = config.Sports.FirstOrDefault(s => s.SportType == "Bike");

        // Assert
        bikeSport.ShouldNotBeNull();
        bikeSport.Name.ShouldBe("Cycling");
        bikeSport.Description.ShouldNotBeEmpty();
        (bikeSport.CarbsPerKgPerHour > 0).ShouldBeTrue();
        (bikeSport.MaxCarbsPerHour > 0).ShouldBeTrue();
    }

    [Fact]
    public void GetConfigurationMetadata_TriathlonSportHasValidConfig()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var triSport = config.Sports.FirstOrDefault(s => s.SportType == "Triathlon");

        // Assert
        triSport.ShouldNotBeNull();
        triSport.Name.ShouldBe("Triathlon");
        triSport.Description.ShouldNotBeEmpty();
    }

    [Fact]
    public void GetConfigurationMetadata_TemperatureAdjustmentsExist()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        config.TemperatureAdjustments.ShouldNotBeNull();
        config.TemperatureAdjustments.Count.ShouldBe(3);
    }

    [Fact]
    public void GetConfigurationMetadata_ColdTemperatureHasAdjustment()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var coldAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Cold");

        // Assert
        coldAdj.ShouldNotBeNull();
        coldAdj.Range.ShouldNotBeEmpty();
        coldAdj.Description.ShouldNotBeEmpty();
        (coldAdj.FluidBonus <= 0).ShouldBeTrue();
    }

    [Fact]
    public void GetConfigurationMetadata_ModerateTemperatureHasAdjustment()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var modAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Moderate");

        // Assert
        modAdj.ShouldNotBeNull();
        modAdj.Range.ShouldNotBeEmpty();
        modAdj.Description.ShouldNotBeEmpty();
        modAdj.FluidBonus.ShouldBe(0);
    }

    [Fact]
    public void GetConfigurationMetadata_HotTemperatureHasAdjustment()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var hotAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Hot");

        // Assert
        hotAdj.ShouldNotBeNull();
        hotAdj.Range.ShouldNotBeEmpty();
        hotAdj.Description.ShouldNotBeEmpty();
        (hotAdj.FluidBonus > 0).ShouldBeTrue();
    }

    [Fact]
    public void GetConfigurationMetadata_AthleteWeightThresholdsExist()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        config.AthleteWeightThresholds.ShouldNotBeNull();
        config.AthleteWeightThresholds.ShouldNotBeEmpty();
    }

    [Fact]
    public void GetConfigurationMetadata_AthleteWeightThresholdsHaveValidValues()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        Assert.All(config.AthleteWeightThresholds, threshold =>
        {
            (threshold.ThresholdKg > 0).ShouldBeTrue();
            threshold.Category.ShouldNotBeEmpty();
            threshold.Description.ShouldNotBeEmpty();
        });
    }

    [Fact]
    public void GetConfigurationMetadata_NutritionTargetsExist()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        config.NutritionTargets.ShouldNotBeNull();
        config.NutritionTargets.ShouldNotBeEmpty();
    }

    [Fact]
    public void GetConfigurationMetadata_NutritionTargetsHaveValidRanges()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        Assert.All(config.NutritionTargets, target =>
        {
            target.Name.ShouldNotBeEmpty();
            target.Unit.ShouldNotBeEmpty();
            target.Description.ShouldNotBeEmpty();
            (target.MaxValue >= target.MinValue).ShouldBeTrue();
        });
    }

    [Fact]
    public void GetConfigurationMetadata_RunHasDifferentCaffeineStartThanBike()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var runSport = config.Sports.FirstOrDefault(s => s.SportType == "Run");
        var bikeSport = config.Sports.FirstOrDefault(s => s.SportType == "Bike");

        // Assert
        runSport.ShouldNotBeNull();
        bikeSport.ShouldNotBeNull();
        bikeSport.CaffeineStartHour.ShouldNotBe(runSport.CaffeineStartHour);
    }

    [Fact]
    public void GetConfigurationMetadata_AllSportsCaffeineConfigValid()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        Assert.All(config.Sports, sport =>
        {
            (sport.CaffeineStartHour >= 0).ShouldBeTrue();
            (sport.CaffeineStartHour < 24).ShouldBeTrue();
            (sport.CaffeineIntervalHours > 0).ShouldBeTrue();
            (sport.MaxCaffeineMgPerKg > 0).ShouldBeTrue();
        });
    }

    [Fact]
    public void GetConfigurationMetadata_AllPhasesHaveValidDescriptions()
    {
        // Act
        var config = _service.GetConfigurationMetadata();

        // Assert
        Assert.All(config.Phases, phase =>
        {
            phase.Name.ShouldNotBeEmpty();
            phase.Description.ShouldNotBeEmpty();
        });
    }

    [Fact]
    public void GetConfigurationMetadata_MultipleCallsReturnConsistentResults()
    {
        // Act
        var config1 = _service.GetConfigurationMetadata();
        var config2 = _service.GetConfigurationMetadata();

        // Assert
        config2.Phases.Count.ShouldBe(config1.Phases.Count);
        config2.Sports.Count.ShouldBe(config1.Sports.Count);
        config2.TemperatureAdjustments.Count.ShouldBe(config1.TemperatureAdjustments.Count);
    }

    [Fact]
    public void GetConfigurationMetadata_BikeCarbsPerKgHigherThanRun()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var runSport = config.Sports.FirstOrDefault(s => s.SportType == "Run");
        var bikeSport = config.Sports.FirstOrDefault(s => s.SportType == "Bike");

        // Assert
        runSport.ShouldNotBeNull();
        bikeSport.ShouldNotBeNull();
        // Cycling requires more carbs per kg than running due to better digestion
        (bikeSport.CarbsPerKgPerHour > runSport.CarbsPerKgPerHour).ShouldBeTrue();
    }

    [Fact]
    public void GetConfigurationMetadata_TemperatureBonusesAreLogical()
    {
        // Act
        var config = _service.GetConfigurationMetadata();
        var coldAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Cold");
        var modAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Moderate");
        var hotAdj = config.TemperatureAdjustments.FirstOrDefault(a => a.TemperatureCondition == "Hot");

        // Assert - Temperature adjustments should increase with heat
        coldAdj.ShouldNotBeNull();
        modAdj.ShouldNotBeNull();
        hotAdj.ShouldNotBeNull();
        (coldAdj.FluidBonus < modAdj.FluidBonus).ShouldBeTrue();
        (modAdj.FluidBonus < hotAdj.FluidBonus).ShouldBeTrue();
    }
}
