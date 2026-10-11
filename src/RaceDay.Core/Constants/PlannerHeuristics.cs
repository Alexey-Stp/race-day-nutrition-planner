namespace RaceDay.Core.Constants;

/// <summary>
/// Product scoring weights and plan-quality thresholds used by the planner.
/// These are heuristics (not physiological limits), grouped here so they are tunable in one place.
/// </summary>
public static class PlannerHeuristics
{
    /// <summary>Weights applied when ranking candidate products for a time slot.</summary>
    public static class Scoring
    {
        public const double CarbWeight = 2.0;
        public const double SodiumFitWeight = 15;

        public const double CaffeineSweetSpotMinMg = 50;
        public const double CaffeineSweetSpotMaxMg = 100;
        public const double CaffeineSweetSpotBonus = 25;
        public const double CaffeineOtherBonus = 10;
        public const double CaffeineOtherWindowFactor = 0.5;

        public const double CaffeineWindow1Bonus = 15;
        public const double CaffeineWindow2Bonus = 20;
        public const double CaffeineWindow3Bonus = 25;
        public const double CaffeineOutsideWindowBonus = 5;

        public const int ConsecutiveUsePenaltyThreshold = 2;
        public const double ConsecutiveUsePenaltyPerUse = 15;
        // Gentle preference for products not yet used much in this plan (variety across the race)
        public const double DiversityPenaltyPerPriorUse = 8;

        public const double IsotonicMinCarbPercent = 6;
        public const double IsotonicMaxCarbPercent = 8;
    }

    /// <summary>Texture suitability per race phase (added to the product score).</summary>
    public static class Suitability
    {
        public const double HighCarbDrinkThresholdG = 30;
        public const double SmallDrinkMaxVolumeMl = 200;

        public const double BikeHighCarbDrink = 50;
        public const double BikeDrink = 30;
        public const double BikeBake = 20;
        public const double BikeChew = 15;
        public const double BikeGel = 10;
        public const double BikeLightGel = 5;

        public const double RunIsotonicGel = 40;
        public const double RunGel = 25;
        public const double RunLightGel = 20;
        public const double RunSmallDrink = 15;
        public const double RunDrink = 5;
        public const double RunBake = -30;
        public const double RunChew = -10;

        public const double SwimIsotonicGel = 20;
        public const double SwimGel = 10;
        public const double SwimOther = -20;
    }

    /// <summary>Thresholds for the post-generation quality diagnostics.</summary>
    public static class Quality
    {
        public const double LateIntakeCheckFraction = 0.70;      // "all carbs before 70% of race"
        public const double AllCarbsEarlyFraction = 0.98;
        public const double FinalSegmentFraction = 2.0 / 3.0;   // start of the final third
        public const int FinalThirdCheckMinDurationMin = 120;
        public const int NoDrinkCheckMinDurationMin = 90;
        public const double LowDiversityFraction = 0.7;
        public const int LowDiversityMinIntakes = 2;
        public const double CaffeineHighWarningFactor = 1.2;
        public const int HydrationCouplingWindowMin = 10;
    }
}
