namespace RaceDay.Core.Models;

/// <summary>
/// Configuration for nutrition planning including caffeine limits
/// </summary>
public static class NutritionConfig
{
    /// <summary>
    /// Maximum caffeine dose in milligrams per kilogram of body weight
    /// </summary>
    public const double MaxCaffeineMgPerKg = 6.0;

    /// <summary>
    /// When to start caffeine intake for running races (in hours from start)
    /// </summary>
    public const double StartCaffeineHourRunning = 1.0;

    /// <summary>
    /// When to start caffeine intake for cycling races (in hours from start)
    /// </summary>
    public const double StartCaffeineHourCycling = 1.5;

    /// <summary>
    /// When to start caffeine intake for half triathlon (in hours from start)
    /// </summary>
    public const double StartCaffeineHourTriathlonHalf = 2.0;

    /// <summary>
    /// When to start caffeine intake for full triathlon (in hours from start)
    /// </summary>
    public const double StartCaffeineHourTriathlonFull = 3.0;

    /// <summary>
    /// Minimum spacing between caffeinated events in minutes
    /// </summary>
    public const int MinCaffeineSpacingMin = 45;

    /// <summary>
    /// Get start caffeine hour for a given race mode
    /// </summary>
    public static double GetStartCaffeineHour(RaceMode mode)
    {
        return mode switch
        {
            RaceMode.Running => StartCaffeineHourRunning,
            RaceMode.Cycling => StartCaffeineHourCycling,
            RaceMode.TriathlonHalf => StartCaffeineHourTriathlonHalf,
            RaceMode.TriathlonFull => StartCaffeineHourTriathlonFull,
            _ => StartCaffeineHourRunning
        };
    }
}
