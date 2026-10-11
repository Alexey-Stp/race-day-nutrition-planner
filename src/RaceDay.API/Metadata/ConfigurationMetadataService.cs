namespace RaceDay.API.Metadata;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>
/// Provides configuration metadata including the calculated constants and their descriptions.
/// </summary>
public interface IConfigurationMetadataService
{
    ConfigurationMetadata GetConfigurationMetadata();
}

/// <summary>
/// Builds configuration metadata from Core constants. Stateless.
/// </summary>
public sealed class ConfigurationMetadataService : IConfigurationMetadataService
{
    /// <summary>
    /// The per-kg carb values and caps below are published guidelines for reference. The planner's
    /// carb target comes from NutritionCalculator (intensity-based, independent of sport and weight).
    /// </summary>
    private const string ReferenceOnlyNote =
        "CarbsPerKgPerHour and MaxCarbsPerHour are reference guidelines only; the planner's carb target is intensity-based (see Carbohydrates).";

    /// <summary>
    /// The slot/caffeine-hour fields are kept for contract compatibility but are not used by the planner.
    /// </summary>
    private static readonly string LegacyTimingNote =
        "SlotIntervalMinutes, CaffeineStartHour, CaffeineIntervalHours and MaxCaffeineMgPerKg are legacy reference values: " +
        "the planner does not use fixed intake slots. Intakes are paced against the carb target subject to minimum spacing (see Interval_* descriptions); " +
        $"caffeine is only scheduled from {SchedulingConstraints.CaffeinePreferredStartPercent * 100}% of race duration, " +
        $"at least {SchedulingConstraints.MinCaffeineSpacing} minutes apart, capped by intensity at " +
        $"{SchedulingConstraints.CaffeineCeilingEasyMgPerKg}/{SchedulingConstraints.CaffeineCeilingModerateMgPerKg}/{SchedulingConstraints.CaffeineCeilingHardMgPerKg} mg/kg " +
        $"(Easy/Moderate/Hard) and {SchedulingConstraints.CaffeineCeilingAbsoluteMaxMg} mg in total.";

    /// <inheritdoc />
    public ConfigurationMetadata GetConfigurationMetadata() =>
        new(
            Phases: GetPhaseInfo(),
            NutritionTargets: GetNutritionTargets(),
            Sports: GetSportConfigurations(),
            TemperatureAdjustments: GetTemperatureAdjustments(),
            AthleteWeightThresholds: GetAthleteWeightThresholds(),
            Descriptions: GetDescriptions()
        );

    private static List<PhaseInfo> GetPhaseInfo() =>
    [
        new PhaseInfo(
            Phase: RacePhase.Swim,
            Name: "Swim",
            Description: "Swimming phase in triathlon - lower intensity nutrition due to difficulty of consuming during water"
        ),
        new PhaseInfo(
            Phase: RacePhase.Bike,
            Name: "Bike",
            Description: "Cycling phase - optimal for consuming nutrition, easier digestion, can handle higher carb intake"
        ),
        new PhaseInfo(
            Phase: RacePhase.Run,
            Name: "Run",
            Description: "Running phase - stomach more sensitive, prefer gels and drinks, avoid solid foods"
        )
    ];

    private static List<NutritionTargetConfig> GetNutritionTargets() =>
    [
        new NutritionTargetConfig(
            Name: "Carbohydrates",
            Unit: "g/hr",
            Description: "Amount of carbohydrates to consume per hour. Set by intensity " +
                         $"({NutritionConstants.Carbohydrates.EasyIntensity}/{NutritionConstants.Carbohydrates.ModerateIntensity}/{NutritionConstants.Carbohydrates.HardIntensity} g/hr for Easy/Moderate/Hard), " +
                         $"+{NutritionConstants.Carbohydrates.LongRaceBonus} g/hr for races over {NutritionConstants.Carbohydrates.LongRaceDurationThreshold} hours at non-easy intensity.",
            MinValue: NutritionConstants.Carbohydrates.EasyIntensity,
            MaxValue: NutritionConstants.Carbohydrates.HardIntensity + NutritionConstants.Carbohydrates.LongRaceBonus,
            BaseValue: NutritionConstants.Carbohydrates.ModerateIntensity
        ),
        new NutritionTargetConfig(
            Name: "Fluids",
            Unit: "ml/hr",
            Description: "Amount of fluid to consume per hour. Adjusted for temperature, athlete weight, and race intensity.",
            MinValue: NutritionConstants.Fluids.MinIntake,
            MaxValue: NutritionConstants.Fluids.MaxIntake,
            BaseValue: NutritionConstants.Fluids.BaseIntake
        ),
        new NutritionTargetConfig(
            Name: "Sodium",
            Unit: "mg/hr",
            Description: "Amount of sodium to maintain electrolyte balance. More critical in hot conditions and for heavier athletes.",
            MinValue: NutritionConstants.Sodium.MinIntake,
            MaxValue: NutritionConstants.Sodium.MaxIntake,
            BaseValue: NutritionConstants.Sodium.BaseIntake
        )
    ];

    private static List<SportConfig> GetSportConfigurations() =>
    [
        new SportConfig(
            SportType: "Run",
            Name: "Running",
            Description: "Road running or trail running events. Lower carb absorption due to stomach motion. " + ReferenceOnlyNote + " " + LegacyTimingNote,
            CarbsPerKgPerHour: AdvancedNutritionConfig.RunningCarbsPerKgPerHour,
            MaxCarbsPerHour: AdvancedNutritionConfig.MaxRunningCarbsPerHour,
            SlotIntervalMinutes: AdvancedNutritionConfig.RunningSlotIntervalMin,
            CaffeineStartHour: AdvancedNutritionConfig.StartCaffeinHourRunning,
            CaffeineIntervalHours: AdvancedNutritionConfig.CaffeineIntervalHours,
            MaxCaffeineMgPerKg: AdvancedNutritionConfig.MaxCaffeineMgPerKg
        ),
        new SportConfig(
            SportType: "Bike",
            Name: "Cycling",
            Description: "Road cycling or triathlon bike portion. Highest carb absorption due to stable position. " + ReferenceOnlyNote + " " + LegacyTimingNote,
            CarbsPerKgPerHour: AdvancedNutritionConfig.CyclingCarbsPerKgPerHour,
            MaxCarbsPerHour: AdvancedNutritionConfig.MaxCyclingCarbsPerHour,
            SlotIntervalMinutes: AdvancedNutritionConfig.CyclingSlotIntervalMin,
            CaffeineStartHour: AdvancedNutritionConfig.StartCaffeinHourCycling,
            CaffeineIntervalHours: AdvancedNutritionConfig.CaffeineIntervalHours,
            MaxCaffeineMgPerKg: AdvancedNutritionConfig.MaxCaffeineMgPerKg
        ),
        new SportConfig(
            SportType: "Triathlon",
            Name: "Triathlon",
            Description: "Multi-sport event combining swim, bike, and run. Balanced nutrition strategy across phases. " + ReferenceOnlyNote + " " + LegacyTimingNote,
            CarbsPerKgPerHour: AdvancedNutritionConfig.TriathlonCarbsPerKgPerHour,
            MaxCarbsPerHour: AdvancedNutritionConfig.MaxTriathlonCarbsPerHour,
            SlotIntervalMinutes: AdvancedNutritionConfig.TriathlonSlotIntervalMin,
            CaffeineStartHour: AdvancedNutritionConfig.StartCaffeinHourTriathlon,
            CaffeineIntervalHours: AdvancedNutritionConfig.CaffeineIntervalHours,
            MaxCaffeineMgPerKg: AdvancedNutritionConfig.MaxCaffeineMgPerKg
        )
    ];

    private static List<TemperatureAdjustment> GetTemperatureAdjustments() =>
    [
        new TemperatureAdjustment(
            TemperatureCondition: "Cold",
            Range: UIMetadataService.TemperatureRangeLabel(TemperatureCondition.Cold),
            FluidBonus: -NutritionConstants.Fluids.ColdWeatherPenalty,
            SodiumBonus: 0,
            Description: "Cold conditions reduce sweat rate and fluid needs. Risk of overconsumption if not adjusted."
        ),
        new TemperatureAdjustment(
            TemperatureCondition: "Moderate",
            Range: UIMetadataService.TemperatureRangeLabel(TemperatureCondition.Moderate),
            FluidBonus: 0,
            SodiumBonus: 0,
            Description: "Moderate temperature conditions. No adjustment needed to base targets."
        ),
        new TemperatureAdjustment(
            TemperatureCondition: "Hot",
            Range: UIMetadataService.TemperatureRangeLabel(TemperatureCondition.Hot),
            FluidBonus: NutritionConstants.Fluids.HotWeatherBonus,
            SodiumBonus: NutritionConstants.Sodium.HotWeatherBonus,
            Description: "Hot conditions increase sweat rate significantly. Increase both fluids and sodium to prevent dehydration and hyponatremia."
        )
    ];

    private static List<AthleteWeightConfig> GetAthleteWeightThresholds() =>
    [
        new AthleteWeightConfig(
            ThresholdKg: NutritionConstants.Weight.LightAthleteThreshold,
            Category: "Light",
            FluidBonus: -NutritionConstants.Fluids.LightAthletePenalty,
            SodiumBonus: 0,
            Description: $"Lighter athletes (< {NutritionConstants.Weight.LightAthleteThreshold} kg) have lower absolute fluid needs. Reduce base targets to prevent overconsumption."
        ),
        new AthleteWeightConfig(
            ThresholdKg: NutritionConstants.Weight.HeavyAthleteThreshold,
            Category: "Heavy",
            FluidBonus: NutritionConstants.Fluids.HeavyAthleteBonus,
            SodiumBonus: NutritionConstants.Sodium.HeavyAthleteBonus,
            Description: $"Heavier athletes (> {NutritionConstants.Weight.HeavyAthleteThreshold} kg) generate more heat and sweat more. Increase fluids and sodium."
        )
    ];

    /// <summary>
    /// General descriptions. Keys are part of the public contract; values describe the rules the planner
    /// actually applies (SchedulingConstraints / PlacementPolicy).
    /// </summary>
    private static Dictionary<string, string> GetDescriptions() => new()
    {
        // Carbohydrate descriptions
        ["CarbTargetMethod"] = "Carb targets are calculated per hour from intensity only " +
            $"(Easy {NutritionConstants.Carbohydrates.EasyIntensity}, Moderate {NutritionConstants.Carbohydrates.ModerateIntensity}, Hard {NutritionConstants.Carbohydrates.HardIntensity} g/hr), " +
            $"plus {NutritionConstants.Carbohydrates.LongRaceBonus} g/hr for races over {NutritionConstants.Carbohydrates.LongRaceDurationThreshold} hours at non-easy intensity. " +
            "Sport type and body weight do not change the carb target; sport-specific per-kg values are reference guidelines only.",
        ["EasyIntensity_Carbs"] = $"Base carbohydrate intake for easy/recovery pace: {NutritionConstants.Carbohydrates.EasyIntensity} g/hr",
        ["ModerateIntensity_Carbs"] = $"Base carbohydrate intake for moderate/steady pace: {NutritionConstants.Carbohydrates.ModerateIntensity} g/hr",
        ["HardIntensity_Carbs"] = $"Base carbohydrate intake for hard/race pace: {NutritionConstants.Carbohydrates.HardIntensity} g/hr",
        ["LongRaceBonus"] = $"Additional carbs for races > {NutritionConstants.Carbohydrates.LongRaceDurationThreshold} hours: +{NutritionConstants.Carbohydrates.LongRaceBonus} g/hr",

        // Phase descriptions
        ["Phase_Pre"] = $"Pre-race: a single intake {AdvancedNutritionConfig.PreRaceNutritionMinutesBefore} minutes before the start, " +
            $"planned only when it is at most {SchedulingConstraints.PreRaceMaxTargetFraction * 100}% of the race carb target",
        ["Phase_Main"] = $"In-race: first intake no earlier than {SchedulingConstraints.EarliestInRaceIntakeMin} minutes; carbs are paced evenly, " +
            $"with at most {SchedulingConstraints.MaxIntakesPerHour} discrete intakes per hour and drink sips of {SchedulingConstraints.SipVolumeMl} ml every {SchedulingConstraints.SipIntervalMinutes} minutes. " +
            $"No more than {SchedulingConstraints.MaxFrontLoadFraction * 100}% of carbs in the first {SchedulingConstraints.FrontLoadWindowFraction * 100}% of the race",
        ["Phase_End"] = $"Finish: no solids in the last {SchedulingConstraints.NoSolidsBeforeFinish} minutes and no intake in the last {SchedulingConstraints.RaceEndSafetyMarginMin} minutes",

        // Spacing rules (keys retained for compatibility; the planner uses minimum spacing, not fixed intervals)
        ["Interval_Triathlon"] = $"Triathlon: no fuelling during the swim or the first {SchedulingConstraints.NoFuelingAfterT1} minutes after T1; " +
            $"no fuelling in the last {AdvancedNutritionConfig.BikeToRunTransitionMarginMin} minutes of the bike (no solids in the last {SchedulingConstraints.NoSolidsBeforeT2}) " +
            $"or the first {SchedulingConstraints.NoFuelingAfterT2} minutes of the run. Bike and run spacing rules apply per leg",
        ["Interval_Cycling"] = $"Cycling minimum spacing: {SchedulingConstraints.MinGelSpacingBike} minutes between gels, {SchedulingConstraints.MinSolidSpacingBike} minutes when a solid is involved, " +
            $"{SchedulingConstraints.MinDrinkSpacing} minutes between whole drinks",
        ["Interval_Running"] = $"Running minimum spacing: {SchedulingConstraints.MinGelSpacingRun} minutes between gels, {SchedulingConstraints.MinSolidSpacingRun} minutes when a solid is involved " +
            $"(bars are bike-only), {SchedulingConstraints.MinDrinkSpacing} minutes between whole drinks",

        // Triathlon phase durations (reference only)
        ["TriathlonPhase_HalfSwim"] = $"Reference Half-Ironman swim duration: {AdvancedNutritionConfig.HalfTriathlonSwimHours} hours (the planner uses explicit legs or distance-preset leg fractions)",
        ["TriathlonPhase_HalfBike"] = $"Reference Half-Ironman bike duration: {AdvancedNutritionConfig.HalfTriathlonBikeHours} hours (the planner uses explicit legs or distance-preset leg fractions)",
        ["TriathlonPhase_FullSwim"] = $"Reference full Ironman swim duration: {AdvancedNutritionConfig.FullTriathlonSwimHours} hours (the planner uses explicit legs or distance-preset leg fractions)",
        ["TriathlonPhase_FullBike"] = $"Reference full Ironman bike duration: {AdvancedNutritionConfig.FullTriathlonBikeHours} hours (the planner uses explicit legs or distance-preset leg fractions)"
    };
}
