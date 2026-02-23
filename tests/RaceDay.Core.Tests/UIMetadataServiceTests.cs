namespace RaceDay.Core.Tests;

using RaceDay.Core.Services;
using RaceDay.Core.Models;

public class UIMetadataServiceTests
{
    [Fact]
    public void GetUIMetadata_ReturnsValidMetadata()
    {
        // Act
        var metadata = UIMetadataService.GetUIMetadata();

        // Assert
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.Temperatures);
        Assert.NotNull(metadata.Intensities);
        Assert.NotEmpty(metadata.DefaultActivityId);
    }

    [Fact]
    public void GetUIMetadata_DefaultActivityIdIsRun()
    {
        // Act
        var metadata = UIMetadataService.GetUIMetadata();

        // Assert
        Assert.Equal("run", metadata.DefaultActivityId);
    }

    [Fact]
    public void GetTemperatureMetadata_ReturnsThreeConditions()
    {
        // Act
        var metadata = UIMetadataService.GetTemperatureMetadata();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(3, metadata.Count);
    }

    [Fact]
    public void GetTemperatureMetadata_ContainsCold()
    {
        // Act
        var metadata = UIMetadataService.GetTemperatureMetadata();
        var coldMeta = metadata.FirstOrDefault(m => m.Condition == TemperatureCondition.Cold);

        // Assert
        Assert.NotNull(coldMeta);
        Assert.Equal("< 5°C", coldMeta.Range);
        Assert.Contains("Reduced fluid needs", coldMeta.Effects);
        Assert.Equal(4, coldMeta.Effects.Length);
    }

    [Fact]
    public void GetTemperatureMetadata_ContainsModerate()
    {
        // Act
        var metadata = UIMetadataService.GetTemperatureMetadata();
        var moderateMeta = metadata.FirstOrDefault(m => m.Condition == TemperatureCondition.Moderate);

        // Assert
        Assert.NotNull(moderateMeta);
        Assert.Equal("5–25°C", moderateMeta.Range);
        Assert.Contains("Baseline nutrition targets", moderateMeta.Effects);
        Assert.Equal(4, moderateMeta.Effects.Length);
    }

    [Fact]
    public void GetTemperatureMetadata_ContainsHot()
    {
        // Act
        var metadata = UIMetadataService.GetTemperatureMetadata();
        var hotMeta = metadata.FirstOrDefault(m => m.Condition == TemperatureCondition.Hot);

        // Assert
        Assert.NotNull(hotMeta);
        Assert.Equal("> 25°C", hotMeta.Range);
        Assert.Contains("Increased fluid needs", hotMeta.Effects);
        Assert.Equal(4, hotMeta.Effects.Length);
    }

    [Fact]
    public void GetTemperatureMetadata_AllHaveEffects()
    {
        // Act
        var metadata = UIMetadataService.GetTemperatureMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            Assert.NotNull(m.Effects);
            Assert.NotEmpty(m.Effects);
            Assert.True(m.Effects.Length > 0);
        });
    }

    [Fact]
    public void GetIntensityMetadata_ReturnsThreeLevels()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(3, metadata.Count);
    }

    [Fact]
    public void GetIntensityMetadata_ContainsEasy()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();
        var easyMeta = metadata.FirstOrDefault(m => m.Level == IntensityLevel.Easy);

        // Assert
        Assert.NotNull(easyMeta);
        Assert.Equal("🟢", easyMeta.Icon);
        Assert.Equal("45 g/hr", easyMeta.CarbRange);
        Assert.Equal("Zone 1-2 (60-75% max HR)", easyMeta.HeartRateZone);
        Assert.Equal(4, easyMeta.Effects.Length);
    }

    [Fact]
    public void GetIntensityMetadata_ContainsModerate()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();
        var moderateMeta = metadata.FirstOrDefault(m => m.Level == IntensityLevel.Moderate);

        // Assert
        Assert.NotNull(moderateMeta);
        Assert.Equal("🟡", moderateMeta.Icon);
        Assert.Equal("75 g/hr", moderateMeta.CarbRange);
        Assert.Equal("Zone 3 (75-85% max HR)", moderateMeta.HeartRateZone);
        Assert.Equal(4, moderateMeta.Effects.Length);
    }

    [Fact]
    public void GetIntensityMetadata_ContainsHard()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();
        var hardMeta = metadata.FirstOrDefault(m => m.Level == IntensityLevel.Hard);

        // Assert
        Assert.NotNull(hardMeta);
        Assert.Equal("🔴", hardMeta.Icon);
        Assert.Equal("105 g/hr", hardMeta.CarbRange);
        Assert.Equal("Zone 4-5 (85-100% max HR)", hardMeta.HeartRateZone);
        Assert.Equal(4, hardMeta.Effects.Length);
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveValidIcons()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            Assert.NotNull(m.Icon);
            Assert.NotEmpty(m.Icon);
        });
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveCarbRanges()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            Assert.NotNull(m.CarbRange);
            Assert.NotEmpty(m.CarbRange);
            Assert.Contains("g/hr", m.CarbRange);
        });
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveHeartRateZones()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            Assert.NotNull(m.HeartRateZone);
            Assert.NotEmpty(m.HeartRateZone);
            Assert.Contains("Zone", m.HeartRateZone);
        });
    }

    [Fact]
    public void GetIntensityMetadata_AllHaveEffects()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();

        // Assert
        Assert.All(metadata, m =>
        {
            Assert.NotNull(m.Effects);
            Assert.NotEmpty(m.Effects);
            Assert.True(m.Effects.Length > 0);
        });
    }

    [Fact]
    public void GetUIMetadata_ContainsBothTemperaturesAndIntensities()
    {
        // Act
        var metadata = UIMetadataService.GetUIMetadata();

        // Assert
        Assert.Equal(3, metadata.Temperatures.Count);
        Assert.Equal(3, metadata.Intensities.Count);
    }

    [Fact]
    public void GetTemperatureMetadata_ColdIsFirst()
    {
        // Act
        var metadata = UIMetadataService.GetTemperatureMetadata();

        // Assert
        Assert.Equal(TemperatureCondition.Cold, metadata[0].Condition);
    }

    [Fact]
    public void GetIntensityMetadata_EasyIsFirst()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();

        // Assert
        Assert.Equal(IntensityLevel.Easy, metadata[0].Level);
    }

    [Fact]
    public void GetTemperatureMetadata_AllConditionsAreUnique()
    {
        // Act
        var metadata = UIMetadataService.GetTemperatureMetadata();
        var conditions = metadata.Select(m => m.Condition).Distinct().Count();

        // Assert
        Assert.Equal(3, conditions);
    }

    [Fact]
    public void GetIntensityMetadata_AllLevelsAreUnique()
    {
        // Act
        var metadata = UIMetadataService.GetIntensityMetadata();
        var levels = metadata.Select(m => m.Level).Distinct().Count();

        // Assert
        Assert.Equal(3, levels);
    }
}
