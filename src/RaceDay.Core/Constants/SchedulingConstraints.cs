namespace RaceDay.Core.Constants;

/// <summary>
/// Scheduling constraints for nutrition intake timing and distribution
/// </summary>
public static class SchedulingConstraints
{
    // Minimum spacing between items (minutes)
    public const int MinGelSpacingBike = 15;
    public const int MinGelSpacingRun = 20;
    public const int MinSolidSpacingBike = 25;
    public const int MinSolidSpacingRun = 30;
    public const int MinDrinkSpacing = 12;
    public const int MinCaffeineSpacing = 45;

    // Clustering prevention (no 2 non-sip items within this window)
    public const int ClusterWindow = 5;

    // Transition zones (minutes)
    public const int NoSolidsBeforeT2 = 15;
    public const int NoFuelingAfterT2 = 5;
    public const int NoSolidsBeforeFinish = 10;

    // Segment distribution targets (ratios)
    public const double TriathlonBikeCarbRatio = 0.70;
    public const double TriathlonRunCarbRatio = 0.30;

    // Action count limits
    public const int MaxIntakesPerHour = 4;
    public const int MaxConsecutiveSameProduct = 3;

    // Target tolerance (percentage)
    public const double TargetTolerancePercent = 0.10; // 10%

    // Caffeine timing (percentage of race duration)
    public const double CaffeinePreferredStartPercent = 0.40; // Start at 40% of race
    public const double CaffeineOptimalWindow1Start = 0.40; // First strategic window: 40-55%
    public const double CaffeineOptimalWindow1End = 0.55;
    public const double CaffeineOptimalWindow2Start = 0.65; // Second strategic window: 65-80%
    public const double CaffeineOptimalWindow2End = 0.80;
    public const double CaffeineOptimalWindow3Start = 0.85; // Final push window: 85-95%
    public const double CaffeineOptimalWindow3End = 0.95;

    // === Sip-based drink scheduling ===
    // Default volume for drinks without explicit VolumeMl
    public const double DefaultDrinkVolumeMl = 500;
    // Sip interval: schedule a sip every N minutes
    public const int SipIntervalMinutes = 10;
    // Volume per sip event (roughly 2 mouthfuls)
    public const double SipVolumeMl = 50;
    // Minimum sip volume to bother scheduling
    public const double MinSipVolumeMl = 20;

    // === Front-loading constraint ===
    // Cannot allocate more than this fraction of total carbs in the first 25% of time
    public const double MaxFrontLoadFraction = 0.40;
    public const double FrontLoadWindowFraction = 0.25;
}
