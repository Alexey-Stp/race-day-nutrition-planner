namespace RaceDay.API.Tests;

using RaceDay.API.Metadata;
using RaceDay.Core.Models;

public class UIMetadataServiceTests
{
    private readonly UIMetadataService _service = new();

    [Fact]
    public void GetUIMetadata_ReturnsValidMetadata()
    {
        // Act
        var metadata = _service.GetUIMetadata();

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Temperatures.ShouldNotBeNull();
        metadata.Intensities.ShouldNotBeNull();
        metadata.DefaultActivityId.ShouldNotBeEmpty();
    }

    [Fact]
    public void GetUIMetadata_DefaultActivityIdIsRun()
    {
        // Act
        var metadata = _service.GetUIMetadata();

        // Assert
        metadata.DefaultActivityId.ShouldBe("run");
    }

    [Fact]
    public void GetTemperatureMetadata_ReturnsThreeConditions()
    {
        // Act
        var metadata = _service.GetTemperatureMetadata();

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Count.ShouldBe(3);
    }

    [Fact]
    public void GetTemperatureMetadata_ContainsCold()
    {
        // Act
        var metadata = _service.GetTemperatureMetadata();
        var coldMeta = metadata.FirstOrDefault(m => m.Condition == TemperatureCondition.Cold);

        // Assert
        coldMeta.ShouldNotBeNull();
        coldMeta.Range.ShouldBe("< 5°C");
        coldMeta.Effects.ShouldContain("Reduced fluid needs");
        coldMeta.Effects.Count.ShouldBe(4);
    }

    [Fact]
    public void GetTemperatureMetadata_ContainsModerate()
    {
        // Act
        var metadata = _service.GetTemperatureMetadata();
        var moderateMeta = metadata.FirstOrDefault(m => m.Condition == TemperatureCondition.Moderate);

        // Assert
        moderateMeta.ShouldNotBeNull();
        moderateMeta.Range.ShouldBe("5–25°C");
        moderateMeta.Effects.ShouldContain("Baseline nutrition targets");
        moderateMeta.Effects.Count.ShouldBe(4);
    }

    [Fact]
    public void GetTemperatureMetadata_ContainsHot()
    {
        // Act
        var metadata = _service.GetTemperatureMetadata();
        var hotMeta = metadata.FirstOrDefault(m => m.Condition == TemperatureCondition.Hot);

        // Assert
        hotMeta.ShouldNotBeNull();
        hotMeta.Range.ShouldBe("> 25°C");
        hotMeta.Effects.ShouldContain("Increased fluid needs");
        hotMeta.Effects.Count.ShouldBe(4);
    }

    [Fact]
    public void GetTemperatureMetadata_AllHaveEffects()
    {
        // Act
        var metadata = _service.GetTemperatureMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            m.Effects.ShouldNotBeNull();
            m.Effects.ShouldNotBeEmpty();
            (m.Effects.Count > 0).ShouldBeTrue();
        });
    }

    [Fact]
    public void GetIntensityMetadata_ReturnsThreeLevels()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();

        // Assert
        metadata.ShouldNotBeNull();
        metadata.Count.ShouldBe(3);
    }

    [Fact]
    public void GetIntensityMetadata_ContainsEasy()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();
        var easyMeta = metadata.FirstOrDefault(m => m.Level == IntensityLevel.Easy);

        // Assert
        easyMeta.ShouldNotBeNull();
        easyMeta.Icon.ShouldBe("🟢");
        easyMeta.CarbRange.ShouldBe("50 g/hr"); // NutritionConstants.Carbohydrates.EasyIntensity
        easyMeta.HeartRateZone.ShouldBe("Zone 1-2 (60-75% max HR)");
        easyMeta.Effects.Count.ShouldBe(4);
    }

    [Fact]
    public void GetIntensityMetadata_ContainsModerate()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();
        var moderateMeta = metadata.FirstOrDefault(m => m.Level == IntensityLevel.Moderate);

        // Assert
        moderateMeta.ShouldNotBeNull();
        moderateMeta.Icon.ShouldBe("🟡");
        moderateMeta.CarbRange.ShouldBe("70 g/hr"); // NutritionConstants.Carbohydrates.ModerateIntensity
        moderateMeta.HeartRateZone.ShouldBe("Zone 3 (75-85% max HR)");
        moderateMeta.Effects.Count.ShouldBe(4);
    }

    [Fact]
    public void GetIntensityMetadata_ContainsHard()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();
        var hardMeta = metadata.FirstOrDefault(m => m.Level == IntensityLevel.Hard);

        // Assert
        hardMeta.ShouldNotBeNull();
        hardMeta.Icon.ShouldBe("🔴");
        hardMeta.CarbRange.ShouldBe("90 g/hr"); // NutritionConstants.Carbohydrates.HardIntensity
        hardMeta.HeartRateZone.ShouldBe("Zone 4-5 (85-100% max HR)");
        hardMeta.Effects.Count.ShouldBe(4);
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveValidIcons()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            m.Icon.ShouldNotBeNull();
            m.Icon.ShouldNotBeEmpty();
        });
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveCarbRanges()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            m.CarbRange.ShouldNotBeNull();
            m.CarbRange.ShouldNotBeEmpty();
            m.CarbRange.ShouldContain("g/hr");
        });
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveHeartRateZones()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            m.HeartRateZone.ShouldNotBeNull();
            m.HeartRateZone.ShouldNotBeEmpty();
            m.HeartRateZone.ShouldContain("Zone");
        });
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveEffects()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            m.Effects.ShouldNotBeNull();
            m.Effects.ShouldNotBeEmpty();
            (m.Effects.Count > 0).ShouldBeTrue();
        });
    }

    [Fact]
    public void GetUIMetadata_ContainsBothTemperaturesAndIntensities()
    {
        // Act
        var metadata = _service.GetUIMetadata();

        // Assert
        metadata.Temperatures.Count.ShouldBe(3);
        metadata.Intensities.Count.ShouldBe(3);
    }

    [Fact]
    public void GetTemperatureMetadata_ColdIsFirst()
    {
        // Act
        var metadata = _service.GetTemperatureMetadata();

        // Assert
        metadata[0].Condition.ShouldBe(TemperatureCondition.Cold);
    }

    [Fact]
    public void GetIntensityMetadata_EasyIsFirst()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();

        // Assert
        metadata[0].Level.ShouldBe(IntensityLevel.Easy);
    }

    [Fact]
    public void GetTemperatureMetadata_AllConditionsAreUnique()
    {
        // Act
        var metadata = _service.GetTemperatureMetadata();
        var conditions = metadata.Select(m => m.Condition).Distinct().Count();

        // Assert
        conditions.ShouldBe(3);
    }

    [Fact]
    public void GetIntensityMetadata_AllLevelsAreUnique()
    {
        // Act
        var metadata = _service.GetIntensityMetadata();
        var levels = metadata.Select(m => m.Level).Distinct().Count();

        // Assert
        levels.ShouldBe(3);
    }
}
