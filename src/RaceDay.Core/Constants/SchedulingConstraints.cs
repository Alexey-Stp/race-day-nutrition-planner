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
    public const int NoFuelingAfterT1 = 5;
    public const int NoFuelingAfterT2 = 5;
    public const int NoSolidsBeforeFinish = 10;

    // Race bounds (minutes)
    public const int RaceEndSafetyMarginMin = 5;     // no intake in the final N minutes
    public const int EarliestInRaceIntakeMin = 10;   // first in-race intake / sip

    // Segment distribution targets (ratios)
    public const double TriathlonBikeCarbRatio = 0.70;
    public const double TriathlonRunCarbRatio = 0.30;

    // Action count limits
    public const int MaxIntakesPerHour = 4;
    public const int MaxConsecutiveSameProduct = 3;

    // Target tolerance (percentage)
    public const double TargetTolerancePercent = 0.10; // 10%
    // Planner aims slightly above target so rounding never lands below it
    public const double TargetAimMarginPercent = 0.02;
    // Stop adding discrete intakes once the remaining need is below this (g)
    public const double MinRemainingCarbsForIntakeG = 5;
    // Pacing: an intake is placed once its carb midpoint fits under the even-distribution curve
    public const double PacingMidpointFraction = 0.5;
    // Pre-race intake is only planned when it is at most this share of the race carb target
    public const double PreRaceMaxTargetFraction = 0.6;

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
    // Only drinks above this carb content per portion form the sip backbone
    public const double BackboneDrinkMinCarbsG = 20;
    // Share of total carbs planned to come from the sip backbone
    public const double DrinkCarbShare = 0.45;

    // === Intensity-based caffeine ceiling (mg per kg bodyweight) ===
    public const double CaffeineCeilingEasyMgPerKg = 1.0;
    public const double CaffeineCeilingModerateMgPerKg = 3.0;
    public const double CaffeineCeilingHardMgPerKg = 4.0;
    // Absolute cap regardless of body weight
    public const double CaffeineCeilingAbsoluteMaxMg = 300;

    // === Full-duration coverage ===
    public const double MinCoverageRatio = 0.85;

    // === Front-loading constraint ===
    // Cannot allocate more than this fraction of total carbs in the first 25% of time
    public const double MaxFrontLoadFraction = 0.40;
    public const double FrontLoadWindowFraction = 0.25;

    // === Temperature condition thresholds (degrees Celsius) ===
    public const double ColdTemperatureThresholdC = 5.0;
    public const double HotTemperatureThresholdC = 25.0;
}
