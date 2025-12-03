namespace RaceDay.Core;

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

        // Temperature adjustments
        if (race.TemperatureC >= NutritionConstants.Temperature.HotThreshold)
            fluids += NutritionConstants.Fluids.HotWeatherBonus;
        
        if (race.TemperatureC <= NutritionConstants.Temperature.ColdThreshold)
            fluids -= NutritionConstants.Fluids.ColdWeatherPenalty;

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

        // Temperature adjustment
        if (race.TemperatureC >= NutritionConstants.Temperature.HotThreshold)
            sodium += NutritionConstants.Sodium.HotWeatherBonus;

        // Weight adjustment
        if (athlete.WeightKg > NutritionConstants.Weight.HeavyAthleteThreshold)
            sodium += NutritionConstants.Sodium.HeavyAthleteBonus;

        // Clamp to safe ranges
        return Math.Clamp(sodium, NutritionConstants.Sodium.MinIntake, NutritionConstants.Sodium.MaxIntake);
    }
}