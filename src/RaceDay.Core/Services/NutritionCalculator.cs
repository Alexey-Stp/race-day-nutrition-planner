namespace RaceDay.Core.Services;
using RaceDay.Core.Models;
using RaceDay.Core.Constants;

/// <summary>
/// Calculates nutrition targets based on athlete and race profiles
/// </summary>
public static class NutritionCalculator
{
    /// <summary>
    /// Calculates carbohydrate, fluid, and sodium targets for the given race and athlete
    /// </summary>
    /// <param name="race">Race profile including sport type, duration, temperature, and intensity</param>
    /// <param name="athlete">Athlete profile including weight</param>
    /// <returns>Nutrition targets with hourly recommendations</returns>
    public static NutritionTargets CalculateTargets(RaceProfile race, AthleteProfile athlete)
    {
        double carbs = CalculateCarbohydrates(race);
        double fluids = CalculateFluids(race, athlete);
        double sodium = CalculateSodium(race, athlete);

        return new NutritionTargets(carbs, fluids, sodium);
    }

    /// <summary>
    /// Calculates comprehensive multi-nutrient targets with segment distribution
    /// </summary>
    public static MultiNutrientTargets CalculateMultiNutrientTargets(
        RaceProfile race, 
        AthleteProfile athlete, 
        bool caffeineEnabled = false)
    {
        var baseTargets = CalculateTargets(race, athlete);
        
        // Calculate totals
        double totalCarbs = baseTargets.CarbsGPerHour * race.DurationHours;
        double totalSodium = baseTargets.SodiumMgPerHour * race.DurationHours;
        double totalFluid = baseTargets.FluidsMlPerHour * race.DurationHours;
        
        // Calculate caffeine target (if enabled) - intensity-based ceiling
        double totalCaffeine = 0;
        if (caffeineEnabled)
        {
            double mgPerKg = race.Intensity switch
            {
                IntensityLevel.Easy => SchedulingConstraints.CaffeineCeilingEasyMgPerKg,
                IntensityLevel.Moderate => SchedulingConstraints.CaffeineCeilingModerateMgPerKg,
                IntensityLevel.Hard => SchedulingConstraints.CaffeineCeilingHardMgPerKg,
                _ => SchedulingConstraints.CaffeineCeilingHardMgPerKg
            };
            totalCaffeine = Math.Min(athlete.WeightKg * mgPerKg, 300);
        }
        
        // Calculate segment-specific targets for triathlon
        Dictionary<RacePhase, PhaseTargets>? segmentTargets = null;
        if (race.SportType == SportType.Triathlon)
        {
            segmentTargets = CalculateTriathlonSegmentTargets(
                race.DurationHours,
                totalCarbs,
                totalSodium,
                totalFluid
            );
        }
        
        return new MultiNutrientTargets(
            CarbsG: totalCarbs,
            SodiumMg: totalSodium,
            FluidMl: totalFluid,
            CaffeineMg: totalCaffeine,
            CarbsPerHour: baseTargets.CarbsGPerHour,
            SodiumPerHour: baseTargets.SodiumMgPerHour,
            FluidPerHour: baseTargets.FluidsMlPerHour,
            SegmentTargets: segmentTargets
        );
    }
    
    private static Dictionary<RacePhase, PhaseTargets> CalculateTriathlonSegmentTargets(
        double totalHours,
        double totalCarbs,
        double totalSodium,
        double totalFluid)
    {
        // Estimate segment durations (percentages)
        const double swimPercent = 0.20;
        const double bikePercent = 0.50;
        const double runPercent = 0.30;
        
        double swimDuration = totalHours * swimPercent * 60; // minutes
        double bikeDuration = totalHours * bikePercent * 60;
        double runDuration = totalHours * runPercent * 60;
        
        // Distribute carbs: 70% bike, 30% run (swim has minimal nutrition)
        double bikeCarbs = totalCarbs * SchedulingConstraints.TriathlonBikeCarbRatio;
        double runCarbs = totalCarbs * SchedulingConstraints.TriathlonRunCarbRatio;
        
        // Distribute sodium and fluid proportionally to duration
        double bikeSodium = totalSodium * bikePercent;
        double runSodium = totalSodium * runPercent;
        double bikeFluid = totalFluid * bikePercent;
        double runFluid = totalFluid * runPercent;
        
        return new Dictionary<RacePhase, PhaseTargets>
        {
            [RacePhase.Swim] = new PhaseTargets(0, 0, 0, swimDuration),
            [RacePhase.Bike] = new PhaseTargets(bikeCarbs, bikeSodium, bikeFluid, bikeDuration),
            [RacePhase.Run] = new PhaseTargets(runCarbs, runSodium, runFluid, runDuration)
        };
    }

    private static double CalculateCarbohydrates(RaceProfile race)
    {
        double carbs = race.Intensity switch
        {
            IntensityLevel.Easy => NutritionConstants.Carbohydrates.EasyIntensity,
            IntensityLevel.Moderate => NutritionConstants.Carbohydrates.ModerateIntensity,
            IntensityLevel.Hard => NutritionConstants.Carbohydrates.HardIntensity,
            _ => NutritionConstants.Carbohydrates.ModerateIntensity
        };

        // Add bonus carbs for long races (non-easy intensity)
        if (race.DurationHours > NutritionConstants.Carbohydrates.LongRaceDurationThreshold 
            && race.Intensity != IntensityLevel.Easy)
        {
            carbs += NutritionConstants.Carbohydrates.LongRaceBonus;
        }

        return carbs;
    }

    private static double CalculateFluids(RaceProfile race, AthleteProfile athlete)
    {
        double fluids = NutritionConstants.Fluids.BaseIntake;

        // Temperature adjustments based on condition
        switch (race.Temperature)
        {
            case TemperatureCondition.Hot:
                fluids += NutritionConstants.Fluids.HotWeatherBonus;
                break;
            case TemperatureCondition.Cold:
                fluids -= NutritionConstants.Fluids.ColdWeatherPenalty;
                break;
            case TemperatureCondition.Moderate:
            default:
                // No adjustment for moderate temperature
                break;
        }

        // Weight adjustments
        if (athlete.WeightKg > NutritionConstants.Weight.HeavyAthleteThreshold)
            fluids += NutritionConstants.Fluids.HeavyAthleteBonus;
        
        if (athlete.WeightKg < NutritionConstants.Weight.LightAthleteThreshold)
            fluids -= NutritionConstants.Fluids.LightAthletePenalty;

        // Clamp to safe ranges
        return Math.Clamp(fluids, NutritionConstants.Fluids.MinIntake, NutritionConstants.Fluids.MaxIntake);
    }

    private static double CalculateSodium(RaceProfile race, AthleteProfile athlete)
    {
        double sodium = NutritionConstants.Sodium.BaseIntake;

        // Temperature adjustment (hot weather increases sodium loss)
        if (race.Temperature == TemperatureCondition.Hot)
            sodium += NutritionConstants.Sodium.HotWeatherBonus;

        // Weight adjustment
        if (athlete.WeightKg > NutritionConstants.Weight.HeavyAthleteThreshold)
            sodium += NutritionConstants.Sodium.HeavyAthleteBonus;

        // Clamp to safe ranges
        return Math.Clamp(sodium, NutritionConstants.Sodium.MinIntake, NutritionConstants.Sodium.MaxIntake);
    }
}