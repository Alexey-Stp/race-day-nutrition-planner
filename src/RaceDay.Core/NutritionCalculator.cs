namespace RaceDay.Core;

public static class NutritionCalculator
{
    public static NutritionTargets CalculateTargets(RaceProfile race, AthleteProfile athlete)
    {
        double carbs = race.Intensity switch
        {
            IntensityLevel.Easy => 50,
            IntensityLevel.Moderate => 70,
            IntensityLevel.Hard => 90,
            _ => 60
        };

        if (race.DurationHours > 5 && race.Intensity != IntensityLevel.Easy)
            carbs += 10;

        double fluids = 500;
        if (race.TemperatureC >= 25) fluids += 200;
        if (race.TemperatureC <= 5) fluids -= 100;

        if (athlete.WeightKg > 80) fluids += 50;
        if (athlete.WeightKg < 60) fluids -= 50;

        fluids = Math.Clamp(fluids, 300, 900);

        double sodium = 400;
        if (race.TemperatureC >= 25) sodium += 200;
        if (athlete.WeightKg > 80) sodium += 100;

        sodium = Math.Clamp(sodium, 300, 1000);

        return new NutritionTargets(carbs, fluids, sodium);
    }
}