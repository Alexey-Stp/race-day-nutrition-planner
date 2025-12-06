namespace RaceDay.Core.Models;

/// <summary>
/// Temperature condition metadata with UI descriptions
/// </summary>
public record TemperatureMetadata(
    TemperatureCondition Condition,
    string Range,
    string[] Effects
);

/// <summary>
/// Intensity level metadata with UI descriptions
/// </summary>
public record IntensityMetadata(
    IntensityLevel Level,
    string Icon,
    string CarbRange,
    string HeartRateZone,
    string[] Effects
);

/// <summary>
/// Container for all UI metadata
/// </summary>
public record UIMetadata(
    List<TemperatureMetadata> Temperatures,
    List<IntensityMetadata> Intensities,
    string DefaultActivityId
);
