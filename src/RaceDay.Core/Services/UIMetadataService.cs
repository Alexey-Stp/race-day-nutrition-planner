namespace RaceDay.Core.Services;

using RaceDay.Core.Models;

/// <summary>
/// Service that provides UI metadata (descriptions, ranges, etc.) for frontend consumption
/// </summary>
public static class UIMetadataService
{
    /// <summary>
    /// Get all UI metadata for the application
    /// </summary>
    public static UIMetadata GetUIMetadata()
    {
        return new UIMetadata(
            Temperatures: GetTemperatureMetadata(),
            Intensities: GetIntensityMetadata(),
            DefaultActivityId: "run"
        );
    }

    /// <summary>
    /// Get temperature condition metadata
    /// </summary>
    public static List<TemperatureMetadata> GetTemperatureMetadata()
    {
        return new List<TemperatureMetadata>
        {
            new TemperatureMetadata(
                Condition: TemperatureCondition.Cold,
                Range: "≤ 5°C",
                Effects: new[]
                {
                    "Reduced fluid needs",
                    "Less sodium required",
                    "Risk of overconsumption",
                    "Lower sweating rate"
                }
            ),
            new TemperatureMetadata(
                Condition: TemperatureCondition.Moderate,
                Range: "5-25°C",
                Effects: new[]
                {
                    "Baseline nutrition targets",
                    "Standard fluid intake",
                    "Optimal conditions",
                    "Stable digestion"
                }
            ),
            new TemperatureMetadata(
                Condition: TemperatureCondition.Hot,
                Range: "≥ 25°C",
                Effects: new[]
                {
                    "Increased fluid needs",
                    "Higher sodium requirements",
                    "Risk of dehydration",
                    "Faster carb absorption"
                }
            )
        };
    }

    /// <summary>
    /// Get intensity level metadata
    /// </summary>
    public static List<IntensityMetadata> GetIntensityMetadata()
    {
        return new List<IntensityMetadata>
        {
            new IntensityMetadata(
                Level: IntensityLevel.Easy,
                Icon: "🟢",
                CarbRange: "45 g/hr",
                HeartRateZone: "Zone 1-2 (60-75% max HR)",
                Effects: new[]
                {
                    "Conversational pace",
                    "Lower carb needs",
                    "Minimal fuel requirements",
                    "Comfortable breathing"
                }
            ),
            new IntensityMetadata(
                Level: IntensityLevel.Moderate,
                Icon: "🟡",
                CarbRange: "75 g/hr",
                HeartRateZone: "Zone 3 (75-85% max HR)",
                Effects: new[]
                {
                    "Steady effort",
                    "Standard nutrition targets",
                    "Regular intake intervals",
                    "Manageable intensity"
                }
            ),
            new IntensityMetadata(
                Level: IntensityLevel.Hard,
                Icon: "🔴",
                CarbRange: "105 g/hr",
                HeartRateZone: "Zone 4-5 (85-100% max HR)",
                Effects: new[]
                {
                    "High effort/competitive",
                    "Maximum carb intake",
                    "Frequent fuel needs",
                    "Elevated heart rate"
                }
            )
        };
    }
}
