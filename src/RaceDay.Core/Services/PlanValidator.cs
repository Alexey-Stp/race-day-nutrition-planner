namespace RaceDay.Core.Services;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;
using Q = RaceDay.Core.Constants.PlannerHeuristics.Quality;

/// <summary>
/// Read-only validation of a finished plan. It never modifies the plan: errors are hard-rule
/// violations (which the planner should never produce), warnings are quality diagnostics.
/// </summary>
internal static class PlanValidator
{
    public sealed record Result(IReadOnlyList<string> Warnings, IReadOnlyList<string> Errors);

    public static Result Validate(IReadOnlyList<PlannedEvent> plan, PlanningContext ctx)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var targets = ctx.Targets;

        double totalCarbs = plan.Sum(e => e.Carbs);
        double totalCaffeine = plan.Sum(e => e.CaffeineMg);

        if (totalCarbs < targets.CarbsG - ctx.CarbTolerance)
            warnings.Add($"Plan underdelivers carbs: {totalCarbs:F0}g < {targets.CarbsG:F0}g target (-{targets.CarbsG - totalCarbs:F0}g). " +
                         "The selected products cannot reach the target within spacing and intake-per-hour limits.");
        else if (totalCarbs > ctx.CarbCap)
            warnings.Add($"Plan overdelivers carbs: {totalCarbs:F0}g > {targets.CarbsG:F0}g target (+{totalCarbs - targets.CarbsG:F0}g)");

        CheckSpacing(plan, errors);
        CheckBounds(plan, ctx, errors);

        if (!ctx.CaffeineEnabled && totalCaffeine > 0)
            errors.Add($"Caffeine disabled but plan contains {totalCaffeine:F0}mg");
        else if (ctx.CaffeineEnabled && totalCaffeine > targets.CaffeineMg * Q.CaffeineHighWarningFactor)
            warnings.Add($"Caffeine high: {totalCaffeine:F0}mg > {targets.CaffeineMg:F0}mg target");

        CheckDiversity(plan, warnings);
        CheckFrontLoad(plan, ctx, warnings);
        warnings.AddRange(CheckHydrationCoupling(plan));
        warnings.AddRange(CheckPlanQuality(plan, ctx.DurationMinutes));

        return new Result(warnings, errors);
    }

    private static void CheckSpacing(IReadOnlyList<PlannedEvent> plan, List<string> errors)
    {
        var intakes = plan.Where(e => !e.IsSip).OrderBy(e => e.TimeMin).ToList();
        for (int i = 0; i < intakes.Count; i++)
        {
            for (int j = i + 1; j < intakes.Count; j++)
            {
                var a = intakes[i];
                var b = intakes[j];
                int required = PlacementPolicy.GetMinimumSpacing(
                    a.Product, b.Product, PlanningContext.SpacingPhase(a.Event.Phase, b.Event.Phase));
                int gap = b.TimeMin - a.TimeMin;
                if (gap < required)
                    errors.Add($"Spacing violation: {a.Product.Name} at {a.TimeMin}min and {b.Product.Name} at {b.TimeMin}min " +
                               $"are {gap}min apart (< {required}min)");
            }
        }
    }

    private static void CheckBounds(IReadOnlyList<PlannedEvent> plan, PlanningContext ctx, List<string> errors)
    {
        foreach (var e in plan)
        {
            if (e.TimeMin > ctx.DurationMinutes)
                errors.Add($"{e.Product.Name} at {e.TimeMin}min is after the finish ({ctx.DurationMinutes}min)");
            if (e.TimeMin < 0 && e.Kind != EventKind.PreRace)
                errors.Add($"{e.Product.Name} at {e.TimeMin}min is before the start");
        }
    }

    private static void CheckDiversity(IReadOnlyList<PlannedEvent> plan, List<string> warnings)
    {
        var intakes = plan.Where(e => !e.IsSip).ToList();
        if (intakes.Count <= Q.LowDiversityMinIntakes)
            return;

        if (intakes.GroupBy(e => e.Product.Name).MaxBy(g => g.Count()) is not { } mostCommon)
            return;
        if (mostCommon.Count() > intakes.Count * Q.LowDiversityFraction)
            warnings.Add($"Low diversity: {mostCommon.Key} used {mostCommon.Count()} times " +
                         $"({mostCommon.Count() * 100 / intakes.Count}% of non-sip events)");
    }

    private static void CheckFrontLoad(IReadOnlyList<PlannedEvent> plan, PlanningContext ctx, List<string> warnings)
    {
        double total = plan.Sum(e => e.Carbs);
        double front = plan.Where(e => e.TimeMin >= 0 && e.TimeMin <= ctx.FrontLoadWindowEndMin).Sum(e => e.Carbs);
        if (total > 0 && front > total * SchedulingConstraints.MaxFrontLoadFraction)
            warnings.Add($"Front-loaded plan: {front:F0}g of {total:F0}g carbs in the first {ctx.FrontLoadWindowEndMin}min " +
                         $"(max {SchedulingConstraints.MaxFrontLoadFraction:P0}).");
    }

    private static List<string> CheckHydrationCoupling(IReadOnlyList<PlannedEvent> plan)
    {
        var warnings = new List<string>();
        foreach (var evt in plan.Where(e => PlacementPolicy.IsGel(e.Product) && !ProductScorer.IsIsotonic(e.Product)))
        {
            bool hasNearbyHydration = plan.Any(other => other.IsSip
                && Math.Abs(other.TimeMin - evt.TimeMin) <= Q.HydrationCouplingWindowMin);
            if (!hasNearbyHydration)
                warnings.Add($"Gel at {evt.TimeMin}min ({evt.Product.Name}) may need additional hydration. " +
                             $"Consider pairing with water or sports drink within {Q.HydrationCouplingWindowMin} minutes.");
        }
        return warnings;
    }

    private static List<string> CheckPlanQuality(IReadOnlyList<PlannedEvent> plan, int durationMinutes)
    {
        var warnings = new List<string>();
        var raceEvents = plan.Where(e => e.TimeMin > 0).ToList();
        if (raceEvents.Count == 0 || durationMinutes <= 0)
            return warnings;

        double totalCarbs = plan.Sum(e => e.Carbs);
        int lateCheckTime = (int)(durationMinutes * Q.LateIntakeCheckFraction);
        int finalThirdStart = (int)(durationMinutes * Q.FinalSegmentFraction);

        double carbsBeforeLateCheck = plan.Where(e => e.TimeMin >= 0 && e.TimeMin <= lateCheckTime).Sum(e => e.Carbs);
        if (totalCarbs > 0 && carbsBeforeLateCheck >= totalCarbs * Q.AllCarbsEarlyFraction)
            warnings.Add($"Front-loaded plan: all carbs consumed before {lateCheckTime}min " +
                         $"({lateCheckTime * 100 / durationMinutes}% of race). Consider spreading intake across the full duration.");

        if (!raceEvents.Any(e => e.TimeMin >= finalThirdStart) && durationMinutes >= Q.FinalThirdCheckMinDurationMin)
            warnings.Add($"No nutrition intake in final third of race (after {finalThirdStart}min). " +
                         "Late-race fueling helps maintain performance.");

        var intakeProducts = plan.Where(e => !e.IsSip && e.TimeMin > 0).Select(e => e.Product).ToList();
        if (intakeProducts.Count > 0 && intakeProducts.All(p => p.CarbsG >= ProductEffectivenessProfiles.LargeGelThresholdG))
            warnings.Add("Only high-carb products used. Consider adding lower-carb options " +
                         "(e.g., lighter gels or chews) for finer distribution across the race.");

        bool hasDrinkEvents = plan.Any(e => e.Product.Texture == ProductTexture.Drink);
        if (!hasDrinkEvents && durationMinutes >= Q.NoDrinkCheckMinDurationMin)
            warnings.Add("No drink products in plan. Consider adding a sports drink for hydration and sodium.");

        return warnings;
    }
}
