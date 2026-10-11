namespace RaceDay.Core.Services;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;
using W = RaceDay.Core.Constants.PlannerHeuristics.Scoring;
using S = RaceDay.Core.Constants.PlannerHeuristics.Suitability;

/// <summary>
/// Ranks candidate products for a given minute. Scoring only expresses preferences; every hard
/// rule (spacing, caffeine, per-hour limits, bounds) is enforced by <see cref="PlacementPolicy"/>.
/// </summary>
internal static class ProductScorer
{
    /// <summary>
    /// Best product that can legally be placed at <paramref name="timeMin"/> without pushing the
    /// non-sip carb total above <paramref name="maxCarbs"/>; null when nothing fits.
    /// </summary>
    public static ProductEnhanced? SelectBest(
        IReadOnlyList<PlannedEvent> plan, int timeMin, PlanningContext ctx, double maxCarbs)
    {
        var phase = ctx.PhaseAt(timeMin);
        double remainingCaffeine = PlacementPolicy.RemainingCaffeineMg(plan, ctx);
        bool caffeineWanted = ctx.CaffeineEnabled && timeMin >= ctx.CaffeineEarliestMin && remainingCaffeine > 0;

        var candidates = ctx.IntakeProducts
            .Where(p => p.CarbsG <= maxCarbs)
            .Where(p => PlacementPolicy.CanPlace(plan, timeMin, p, ctx))
            .Select(p => (Product: p, Score: Score(p, phase, plan, timeMin, ctx, remainingCaffeine)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (candidates.Count == 0)
            return null;

        // Prefer a caffeinated option while the remaining ceiling still covers a full portion of it.
        if (caffeineWanted)
        {
            var caffeinated = candidates.FirstOrDefault(c => c.Product.HasCaffeine && c.Product.CaffeineMg <= remainingCaffeine);
            if (caffeinated.Product != null)
                return caffeinated.Product;
        }

        return candidates[0].Product;
    }

    public static double Score(
        ProductEnhanced product,
        RacePhase phase,
        IReadOnlyList<PlannedEvent> plan,
        int timeMin,
        PlanningContext ctx,
        double remainingCaffeineMg)
    {
        double raceProgress = ctx.DurationMinutes > 0 ? (double)timeMin / ctx.DurationMinutes : 0;
        double score = product.CarbsG * W.CarbWeight + GetSegmentSuitabilityScore(product, phase);

        double sodiumSoFar = plan.Sum(e => e.Product.SodiumMg * e.Event.AmountPortions);
        double remainingSodium = ctx.Targets.SodiumMg - sodiumSoFar;
        if (remainingSodium > 0)
            score += Math.Min(product.SodiumMg / remainingSodium, 1.0) * W.SodiumFitWeight;

        if (product.HasCaffeine && remainingCaffeineMg > 0 && raceProgress >= SchedulingConstraints.CaffeinePreferredStartPercent)
        {
            double windowBonus = CalculateCaffeineWindowBonus(raceProgress);
            score += product.CaffeineMg is >= W.CaffeineSweetSpotMinMg and <= W.CaffeineSweetSpotMaxMg
                ? W.CaffeineSweetSpotBonus + windowBonus
                : W.CaffeineOtherBonus + windowBonus * W.CaffeineOtherWindowFactor;
        }

        var intakesBefore = plan.Where(e => !e.IsSip && e.TimeMin < timeMin).OrderBy(e => e.TimeMin).ToList();
        int consecutiveUses = intakesBefore.AsEnumerable().Reverse().TakeWhile(e => e.Product.Name == product.Name).Count();
        if (consecutiveUses >= W.ConsecutiveUsePenaltyThreshold)
            score -= consecutiveUses * W.ConsecutiveUsePenaltyPerUse;
        score -= plan.Count(e => !e.IsSip && e.Product.Name == product.Name) * W.DiversityPenaltyPerPriorUse;

        var lastIntake = intakesBefore.LastOrDefault();
        var lastSip = plan.Where(e => e.IsSip && e.TimeMin < timeMin).MaxBy(e => e.TimeMin);
        var dynamicContext = new ProductSelectionContext(
            LastNonSipProduct: lastIntake?.Product,
            LastNonSipTimeMin: lastIntake?.TimeMin ?? -1,
            LastDrinkTimeMin: lastSip?.TimeMin ?? -1,
            CurrentTimeMin: timeMin,
            CurrentPhase: phase);
        score += DynamicSelectionStrategy.CalculateTotalDynamicBonus(product, dynamicContext);

        return score;
    }

    public static double GetSegmentSuitabilityScore(ProductEnhanced product, RacePhase segment) =>
        segment switch
        {
            RacePhase.Bike => product.Texture switch
            {
                ProductTexture.Drink when product.CarbsG > S.HighCarbDrinkThresholdG => S.BikeHighCarbDrink,
                ProductTexture.Drink => S.BikeDrink,
                ProductTexture.Bake => S.BikeBake,
                ProductTexture.Chew => S.BikeChew,
                ProductTexture.Gel => S.BikeGel,
                ProductTexture.LightGel => S.BikeLightGel,
                _ => 0
            },
            RacePhase.Run => product.Texture switch
            {
                ProductTexture.Gel when IsIsotonic(product) => S.RunIsotonicGel,
                ProductTexture.Gel => S.RunGel,
                ProductTexture.LightGel => S.RunLightGel,
                ProductTexture.Drink when product.VolumeMl <= S.SmallDrinkMaxVolumeMl => S.RunSmallDrink,
                ProductTexture.Drink => S.RunDrink,
                ProductTexture.Bake => S.RunBake,
                ProductTexture.Chew => S.RunChew,
                _ => 0
            },
            RacePhase.Swim => product.Texture switch
            {
                ProductTexture.Gel when IsIsotonic(product) => S.SwimIsotonicGel,
                ProductTexture.Gel => S.SwimGel,
                _ => S.SwimOther
            },
            _ => 0
        };

    public static bool IsIsotonic(ProductEnhanced product)
    {
        if (product.VolumeMl > 0)
        {
            double carbPercent = product.CarbsG / (product.VolumeMl / 1000.0) / 10.0;
            return carbPercent is >= W.IsotonicMinCarbPercent and <= W.IsotonicMaxCarbPercent;
        }
        return product.ProductType?.Contains("isotonic", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private static double CalculateCaffeineWindowBonus(double raceProgress)
    {
        if (raceProgress is >= SchedulingConstraints.CaffeineOptimalWindow1Start and <= SchedulingConstraints.CaffeineOptimalWindow1End)
            return W.CaffeineWindow1Bonus;
        if (raceProgress is >= SchedulingConstraints.CaffeineOptimalWindow2Start and <= SchedulingConstraints.CaffeineOptimalWindow2End)
            return W.CaffeineWindow2Bonus;
        if (raceProgress is >= SchedulingConstraints.CaffeineOptimalWindow3Start and <= SchedulingConstraints.CaffeineOptimalWindow3End)
            return W.CaffeineWindow3Bonus;
        return raceProgress >= SchedulingConstraints.CaffeinePreferredStartPercent ? W.CaffeineOutsideWindowBonus : 0;
    }
}
