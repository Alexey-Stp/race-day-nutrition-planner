namespace RaceDay.Core.Constants;

/// <summary>
/// Advanced nutrition configuration with sport-specific parameters
/// </summary>
public static class AdvancedNutritionConfig
{
    // Carbs per kg per hour by sport mode
    public const double TriathlonCarbsPerKgPerHour = 1.3;
    public const double CyclingCarbsPerKgPerHour = 1.4;
    public const double RunningCarbsPerKgPerHour = 1.2;

    // Hard caps (max carbs per hour)
    public const double MaxTriathlonCarbsPerHour = 95;
    public const double MaxCyclingCarbsPerHour = 100;
    public const double MaxRunningCarbsPerHour = 90;

    // Intake slot intervals in minutes
    public const int TriathlonSlotIntervalMin = 25;
    public const int CyclingSlotIntervalMin = 22;
    public const int RunningSlotIntervalMin = 27;

    // Triathlon phase durations in hours
    public const double HalfTriathlonSwimHours = 0.5;
    public const double HalfTriathlonBikeHours = 2.75;
    public const double FullTriathlonSwimHours = 1.0;
    public const double FullTriathlonBikeHours = 5.0;

    // Caffeine strategy
    public const double StartCaffeinHourTriathlon = 1.5;
    public const double StartCaffeinHourCycling = 1.0;
    public const double StartCaffeinHourRunning = 1.5;
    public const double CaffeineIntervalHours = 0.75;
    public const double MaxCaffeineMgPerKg = 5.0;

    // End phase detection
    public const double EndPhaseThreshold = 0.8; // After 80% of race
}
