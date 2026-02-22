namespace RaceDay.Core.Services;

using RaceDay.Core.Models;
using RaceDay.Core.Constants;

/// <summary>
/// Service that provides configuration metadata including all calculated constants and their descriptions
/// </summary>
public static class ConfigurationMetadataService
{
    /// <summary>
    /// Get complete configuration metadata for frontend consumption
    /// </summary>
    public static ConfigurationMetadata GetConfigurationMetadata()
    {
        return new ConfigurationMetadata(
            Phases: GetPhaseInfo(),
            NutritionTargets: GetNutritionTargets(),
            Sports: GetSportConfigurations(),
            TemperatureAdjustments: GetTemperatureAdjustments(),
            AthleteWeightThresholds: GetAthleteWeightThresholds(),
            Descriptions: GetDescriptions()
        );
    }

    /// <summary>
    /// Get phase information with descriptions
    /// </summary>
    public static List<PhaseInfo> GetPhaseInfo()
    {
        return new List<PhaseInfo>
        {
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
        };
    }

    /// <summary>
    /// Get nutrition target configurations
    /// </summary>
    public static List<NutritionTargetConfig> GetNutritionTargets()
    {
        return new List<NutritionTargetConfig>
        {
            new NutritionTargetConfig(
                Name: "Carbohydrates",
                Unit: "g/hr",
                Description: "Amount of carbohydrates to consume per hour. Higher for intense efforts, adjusted based on race type and duration.",
                MinValue: 30,
                MaxValue: 100,
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
        };
    }

    /// <summary>
    /// Get sport-specific configurations
    /// </summary>
    public static List<SportConfig> GetSportConfigurations()
    {
        return new List<SportConfig>
        {
            new SportConfig(
                SportType: "Run",
                Name: "Running",
                Description: "Road running or trail running events. Lower carb absorption due to stomach motion.",
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
                Description: "Road cycling or triathlon bike portion. Highest carb absorption due to stable position.",
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
                Description: "Multi-sport event combining swim, bike, and run. Balanced nutrition strategy across phases.",
                CarbsPerKgPerHour: AdvancedNutritionConfig.TriathlonCarbsPerKgPerHour,
                MaxCarbsPerHour: AdvancedNutritionConfig.MaxTriathlonCarbsPerHour,
                SlotIntervalMinutes: AdvancedNutritionConfig.TriathlonSlotIntervalMin,
                CaffeineStartHour: AdvancedNutritionConfig.StartCaffeinHourTriathlon,
                CaffeineIntervalHours: AdvancedNutritionConfig.CaffeineIntervalHours,
                MaxCaffeineMgPerKg: AdvancedNutritionConfig.MaxCaffeineMgPerKg
            )
        };
    }

    /// <summary>
    /// Get temperature-based adjustment configurations
    /// </summary>
    public static List<TemperatureAdjustment> GetTemperatureAdjustments()
    {
        return new List<TemperatureAdjustment>
        {
            new TemperatureAdjustment(
                TemperatureCondition: "Cold",
                Range: "5°C",
                FluidBonus: -NutritionConstants.Fluids.ColdWeatherPenalty,
                SodiumBonus: 0,
                Description: "Cold conditions reduce sweat rate and fluid needs. Risk of overconsumption if not adjusted."
            ),
            new TemperatureAdjustment(
                TemperatureCondition: "Moderate",
                Range: "5-25°C",
                FluidBonus: 0,
                SodiumBonus: 0,
                Description: "Moderate temperature conditions. No adjustment needed to base targets."
            ),
            new TemperatureAdjustment(
                TemperatureCondition: "Hot",
                Range: "25°C",
                FluidBonus: NutritionConstants.Fluids.HotWeatherBonus,
                SodiumBonus: NutritionConstants.Sodium.HotWeatherBonus,
                Description: "Hot conditions increase sweat rate significantly. Increase both fluids and sodium to prevent dehydration and hyponatremia."
            )
        };
    }

    /// <summary>
    /// Get athlete weight-based adjustment configurations
    /// </summary>
    public static List<AthleteWeightConfig> GetAthleteWeightThresholds()
    {
        return new List<AthleteWeightConfig>
        {
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
        };
    }

    /// <summary>
    /// Get general descriptions for configuration parameters
    /// </summary>
    public static Dictionary<string, string> GetDescriptions()
    {
        return new Dictionary<string, string>
        {
            // Carbohydrate descriptions
            ["EasyIntensity_Carbs"] = $"Base carbohydrate intake for easy/recovery pace: {NutritionConstants.Carbohydrates.EasyIntensity} g/hr",
            ["ModerateIntensity_Carbs"] = $"Base carbohydrate intake for moderate/steady pace: {NutritionConstants.Carbohydrates.ModerateIntensity} g/hr",
            ["HardIntensity_Carbs"] = $"Base carbohydrate intake for hard/race pace: {NutritionConstants.Carbohydrates.HardIntensity} g/hr",
            ["LongRaceBonus"] = $"Additional carbs for races > {NutritionConstants.Carbohydrates.LongRaceDurationThreshold} hours: +{NutritionConstants.Carbohydrates.LongRaceBonus} g/hr",

            // Phase descriptions
            ["Phase_Pre"] = "Pre-race or warm-up phase - light intake to settle stomach",
            ["Phase_Main"] = "Main race phase - maximum nutrition intake",
            ["Phase_End"] = $"End phase (after {AdvancedNutritionConfig.EndPhaseThreshold * 100}% of race) - reduce intake for finishing",

            // Interval descriptions
            ["Interval_Triathlon"] = $"Nutrition intake interval for triathlon: {AdvancedNutritionConfig.TriathlonSlotIntervalMin} minutes",
            ["Interval_Cycling"] = $"Nutrition intake interval for cycling: {AdvancedNutritionConfig.CyclingSlotIntervalMin} minutes",
            ["Interval_Running"] = $"Nutrition intake interval for running: {AdvancedNutritionConfig.RunningSlotIntervalMin} minutes",

            // Triathlon phase durations
            ["TriathlonPhase_HalfSwim"] = $"Half-Ironman swim duration: {AdvancedNutritionConfig.HalfTriathlonSwimHours} hours",
            ["TriathlonPhase_HalfBike"] = $"Half-Ironman bike duration: {AdvancedNutritionConfig.HalfTriathlonBikeHours} hours",
            ["TriathlonPhase_FullSwim"] = $"Full Ironman swim duration: {AdvancedNutritionConfig.FullTriathlonSwimHours} hours",
            ["TriathlonPhase_FullBike"] = $"Full Ironman bike duration: {AdvancedNutritionConfig.FullTriathlonBikeHours} hours"
        };
    }
}
