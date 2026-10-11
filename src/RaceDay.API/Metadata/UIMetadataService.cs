namespace RaceDay.API.Metadata;

using System.Globalization;
using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>
/// Provides UI metadata (descriptions, ranges, etc.) for frontend consumption.
/// </summary>
public interface IUIMetadataService
{
    UIMetadata GetUIMetadata();
    IReadOnlyList<TemperatureMetadata> GetTemperatureMetadata();
    IReadOnlyList<IntensityMetadata> GetIntensityMetadata();
}

/// <summary>
/// Builds UI metadata from Core constants. Stateless.
/// </summary>
public sealed class UIMetadataService : IUIMetadataService
{
    /// <summary>Activity the UI selects on first load.</summary>
    public const string DefaultActivityId = "run";

    /// <summary>
    /// Display label for the temperature band of a condition, built from the classification thresholds.
    /// </summary>
    public static string TemperatureRangeLabel(TemperatureCondition condition)
    {
        var cold = SchedulingConstraints.ColdTemperatureThresholdC.ToString(CultureInfo.InvariantCulture);
        var hot = SchedulingConstraints.HotTemperatureThresholdC.ToString(CultureInfo.InvariantCulture);
        return condition switch
        {
            TemperatureCondition.Cold => $"< {cold}°C",
            TemperatureCondition.Hot => $"> {hot}°C",
            _ => $"{cold}–{hot}°C"
        };
    }

    /// <summary>
    /// Carb target label for an intensity, taken from the values NutritionCalculator uses.
    /// </summary>
    public static string CarbRangeLabel(IntensityLevel level)
    {
        double carbs = level switch
        {
            IntensityLevel.Easy => NutritionConstants.Carbohydrates.EasyIntensity,
            IntensityLevel.Hard => NutritionConstants.Carbohydrates.HardIntensity,
            _ => NutritionConstants.Carbohydrates.ModerateIntensity
        };
        return $"{carbs.ToString(CultureInfo.InvariantCulture)} g/hr";
    }

    /// <inheritdoc />
    public UIMetadata GetUIMetadata() =>
        new(
            Temperatures: GetTemperatureMetadata(),
            Intensities: GetIntensityMetadata(),
            DefaultActivityId: DefaultActivityId
        );

    /// <inheritdoc />
    public IReadOnlyList<TemperatureMetadata> GetTemperatureMetadata() =>
    [
        new TemperatureMetadata(
            Condition: TemperatureCondition.Cold,
            Range: TemperatureRangeLabel(TemperatureCondition.Cold),
            Effects:
            [
                "Reduced fluid needs",
                "Less sodium required",
                "Risk of overconsumption",
                "Lower sweating rate"
            ]
        ),
        new TemperatureMetadata(
            Condition: TemperatureCondition.Moderate,
            Range: TemperatureRangeLabel(TemperatureCondition.Moderate),
            Effects:
            [
                "Baseline nutrition targets",
                "Standard fluid intake",
                "Optimal conditions",
                "Stable digestion"
            ]
        ),
        new TemperatureMetadata(
            Condition: TemperatureCondition.Hot,
            Range: TemperatureRangeLabel(TemperatureCondition.Hot),
            Effects:
            [
                "Increased fluid needs",
                "Higher sodium requirements",
                "Risk of dehydration",
                "Faster carb absorption"
            ]
        )
    ];

    /// <inheritdoc />
    public IReadOnlyList<IntensityMetadata> GetIntensityMetadata() =>
    [
        new IntensityMetadata(
            Level: IntensityLevel.Easy,
            Icon: "🟢",
            CarbRange: CarbRangeLabel(IntensityLevel.Easy),
            HeartRateZone: "Zone 1-2 (60-75% max HR)",
            Effects:
            [
                "Conversational pace",
                "Lower carb needs",
                "Minimal fuel requirements",
                "Comfortable breathing"
            ]
        ),
        new IntensityMetadata(
            Level: IntensityLevel.Moderate,
            Icon: "🟡",
            CarbRange: CarbRangeLabel(IntensityLevel.Moderate),
            HeartRateZone: "Zone 3 (75-85% max HR)",
            Effects:
            [
                "Steady effort",
                "Standard nutrition targets",
                "Regular intake intervals",
                "Manageable intensity"
            ]
        ),
        new IntensityMetadata(
            Level: IntensityLevel.Hard,
            Icon: "🔴",
            CarbRange: CarbRangeLabel(IntensityLevel.Hard),
            HeartRateZone: "Zone 4-5 (85-100% max HR)",
            Effects:
            [
                "High effort/competitive",
                "Maximum carb intake",
                "Frequent fuel needs",
                "Elevated heart rate"
            ]
        )
    ];
}
