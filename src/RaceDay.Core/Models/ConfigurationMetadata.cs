namespace RaceDay.Core.Models;

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
/// Sport-specific configuration
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
    List<PhaseInfo> Phases,
    List<NutritionTargetConfig> NutritionTargets,
    List<SportConfig> Sports,
    List<TemperatureAdjustment> TemperatureAdjustments,
    List<AthleteWeightConfig> AthleteWeightThresholds,
    Dictionary<string, string> Descriptions
);
