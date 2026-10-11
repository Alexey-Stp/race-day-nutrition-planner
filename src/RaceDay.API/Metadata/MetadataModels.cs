namespace RaceDay.API.Metadata;

using RaceDay.Core.Models;

/// <summary>
/// Temperature condition metadata with UI descriptions
/// </summary>
public record TemperatureMetadata(
    TemperatureCondition Condition,
    string Range,
    IReadOnlyList<string> Effects
);

/// <summary>
/// Intensity level metadata with UI descriptions
/// </summary>
public record IntensityMetadata(
    IntensityLevel Level,
    string Icon,
    string CarbRange,
    string HeartRateZone,
    IReadOnlyList<string> Effects
);

/// <summary>
/// Container for all UI metadata
/// </summary>
public record UIMetadata(
    IReadOnlyList<TemperatureMetadata> Temperatures,
    IReadOnlyList<IntensityMetadata> Intensities,
    string DefaultActivityId
);

/// <summary>
/// Phase-specific information with description
/// </summary>
public record PhaseInfo(
    RacePhase Phase,
    string Name,
    string Description
);

/// <summary>
/// Nutrition target configuration with descriptions
/// </summary>
public record NutritionTargetConfig(
    string Name,
    string Unit,
    string Description,
    double MinValue,
    double MaxValue,
    double BaseValue
);

/// <summary>
/// Sport-specific configuration. <see cref="SlotIntervalMinutes"/>, <see cref="CaffeineStartHour"/>,
/// <see cref="CaffeineIntervalHours"/> and <see cref="MaxCaffeineMgPerKg"/> are legacy reference values kept
/// for contract compatibility; the planner applies the rules described in <see cref="ConfigurationMetadata.Descriptions"/>.
/// </summary>
public record SportConfig(
    string SportType,
    string Name,
    string Description,
    double CarbsPerKgPerHour,
    double MaxCarbsPerHour,
    int SlotIntervalMinutes,
    double CaffeineStartHour,
    double CaffeineIntervalHours,
    double MaxCaffeineMgPerKg
);

/// <summary>
/// Temperature-based adjustment configuration
/// </summary>
public record TemperatureAdjustment(
    string TemperatureCondition,
    string Range,
    double FluidBonus,
    double SodiumBonus,
    string Description
);

/// <summary>
/// Athlete weight-based configuration
/// </summary>
public record AthleteWeightConfig(
    double ThresholdKg,
    string Category,
    double FluidBonus,
    double SodiumBonus,
    string Description
);

/// <summary>
/// Complete nutrition configuration for client consumption
/// </summary>
public record ConfigurationMetadata(
    IReadOnlyList<PhaseInfo> Phases,
    IReadOnlyList<NutritionTargetConfig> NutritionTargets,
    IReadOnlyList<SportConfig> Sports,
    IReadOnlyList<TemperatureAdjustment> TemperatureAdjustments,
    IReadOnlyList<AthleteWeightConfig> AthleteWeightThresholds,
    IReadOnlyDictionary<string, string> Descriptions
);
