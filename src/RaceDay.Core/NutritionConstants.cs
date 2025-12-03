namespace RaceDay.Core;

/// <summary>
/// Contains nutritional calculation constants and thresholds
/// </summary>
public static class NutritionConstants
{
    /// <summary>
    /// Carbohydrate intake constants (grams per hour)
    /// </summary>
    public static class Carbohydrates
    {
        public const double EasyIntensity = 50;
        public const double ModerateIntensity = 70;
        public const double HardIntensity = 90;
        public const double LongRaceBonus = 10;
        public const double LongRaceDurationThreshold = 5.0; // hours
    }

    /// <summary>
    /// Fluid intake constants (milliliters per hour)
    /// </summary>
    public static class Fluids
    {
        public const double BaseIntake = 500;
        public const double HotWeatherBonus = 200;
        public const double ColdWeatherPenalty = 100;
        public const double HeavyAthleteBonus = 50;
        public const double LightAthletePenalty = 50;
        public const double MinIntake = 300;
        public const double MaxIntake = 900;
    }

    /// <summary>
    /// Sodium intake constants (milligrams per hour)
    /// </summary>
    public static class Sodium
    {
        public const double BaseIntake = 400;
        public const double HotWeatherBonus = 200;
        public const double HeavyAthleteBonus = 100;
        public const double MinIntake = 300;
        public const double MaxIntake = 1000;
    }

    /// <summary>
    /// Temperature thresholds (degrees Celsius)
    /// </summary>
    public static class Temperature
    {
        public const double HotThreshold = 25;
        public const double ColdThreshold = 5;
    }

    /// <summary>
    /// Athlete weight thresholds (kilograms)
    /// </summary>
    public static class Weight
    {
        public const double HeavyAthleteThreshold = 80;
        public const double LightAthleteThreshold = 60;
    }
}
