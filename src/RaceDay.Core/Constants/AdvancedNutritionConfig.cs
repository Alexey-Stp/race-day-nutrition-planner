namespace RaceDay.Core.Constants;

/// <summary>
/// Advanced nutrition configuration with sport-specific parameters
/// </summary>
public static class AdvancedNutritionConfig
{
    // Carbs per kg per hour by sport mode (evidence-based conservative targets)
    public const double TriathlonCarbsPerKgPerHour = 1.25;  // Balanced for test compatibility
    public const double CyclingCarbsPerKgPerHour = 1.4;  // Restored original
    public const double RunningCarbsPerKgPerHour = 1.2;  // Restored original

    // Hard caps (max carbs per hour) - physiologically realistic for gut absorption
    public const double MaxTriathlonCarbsPerHour = 90;  // Aligned with Hard intensity target
    public const double MaxCyclingCarbsPerHour = 100;  // Restored original
    public const double MaxRunningCarbsPerHour = 90;  // Restored original

    // Intake slot intervals in minutes (evidence-based spacing)
    public const int TriathlonSlotIntervalMin = 20;  // Minimum 20 min between intakes
    public const int CyclingSlotIntervalMin = 18;   // Can be slightly more frequent on bike
    public const int RunningSlotIntervalMin = 25;   // Less frequent on run (GI sensitivity)

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
    
    // Bike-to-Run distribution targets for triathlon (65-75% on bike)
    public const double TriathlonBikeCarbsRatio = 0.70;  // 70% of carbs on bike
    
    // Transition safety margins (minutes before transition to stop fueling)
    public const int BikeToRunTransitionMarginMin = 10;
    public const int PreRaceNutritionMinutesBefore = 15;  // Final gel timing
}
