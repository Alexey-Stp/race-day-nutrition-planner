namespace RaceDay.Core.Services;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>
/// Nutrition planner entry point. Builds a plan in a fixed pipeline where every inserted or moved
/// event is created by <see cref="NutritionEventFactory"/> and admitted by <see cref="PlacementPolicy"/>:
/// <list type="number">
/// <item>Pre-race intake (only when it is a modest share of the race target).</item>
/// <item>Paced discrete intakes per fuel window, following an even-distribution curve.</item>
/// <item>Sip backbone sized to close the remaining gap to the carb aim.</item>
/// <item>Deficit fill (only when sips at full volume cannot reach the target), overshoot trim,
/// coverage and front-load guards — all of which move or add events under the same policy.</item>
/// <item>One cumulative-total pass, then one validation pass.</item>
/// </list>
/// </summary>
public class PlanGenerator
{
    private const double CarbComparisonSlackG = 0.05;
    private const int OneHourMin = 60;

    // ─── Public API ──────────────────────────────────────────────────

    public List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        IReadOnlyList<ProductEnhanced> products,
        bool caffeineEnabled = false) =>
        GeneratePlanWithDiagnostics(race, athlete, products, caffeineEnabled).Events.ToList();

    public PlanResult GeneratePlanWithDiagnostics(
        RaceProfile race,
        AthleteProfile athlete,
        IReadOnlyList<ProductEnhanced> products,
        bool caffeineEnabled = false)
    {
        var (legModel, legWarnings) = ResolveLegModel(race);
        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled, legModel);
        var ctx = new PlanningContext(race, products, caffeineEnabled, legModel, targets);

        var plan = BuildPlan(ctx);
        var validation = PlanValidator.Validate(plan, ctx);

        return new PlanResult(
            Finalize(plan),
            legWarnings.Concat(validation.Warnings).ToList(),
            validation.Errors,
            targets);
    }

    /// <summary>Hours → whole minutes, rounded (4.1 h is 246 min, not the truncated 245).</summary>
    internal static int ToMinutes(double hours) => (int)Math.Round(hours * 60, MidpointRounding.AwayFromZero);

    // ─── Pipeline ────────────────────────────────────────────────────

    internal static List<PlannedEvent> BuildPlan(PlanningContext ctx)
    {
        var plan = new List<PlannedEvent>();
        AddPreRaceIntake(plan, ctx);
        PlaceIntakesPaced(plan, ctx);
        RebuildSipBackbone(plan, ctx);
        FillDeficit(plan, ctx);
        TrimOvershoot(plan, ctx);
        EnsureCoverage(plan, ctx);
        EnforceFrontLoadConstraint(plan, ctx);
        return plan;
    }

    // ─── Phase 1: Pre-race intake ────────────────────────────────────

    private static void AddPreRaceIntake(List<PlannedEvent> plan, PlanningContext ctx)
    {
        double maxCarbs = ctx.Targets.CarbsG * SchedulingConstraints.PreRaceMaxTargetFraction;
        var candidates = ctx.UsableProducts.Where(p => !p.HasCaffeine && p.CarbsG <= maxCarbs).ToList();
        var product = candidates.FirstOrDefault(p => p.Texture == ProductTexture.Bake)
                      ?? candidates.FirstOrDefault(p => p.Texture == ProductTexture.Gel)
                      ?? candidates.FirstOrDefault(p => p.Texture == ProductTexture.LightGel);
        if (product != null)
            plan.Add(NutritionEventFactory.PreRace(product, ctx.PreRaceTimeMin, ctx.MainPhase));
    }

    // ─── Phase 2: Paced discrete intakes ─────────────────────────────

    /// <summary>
    /// Walks each fuel window minute by minute and places an intake when the window's running
    /// total (plus half the candidate's carbs) fits under a straight-line distribution curve.
    /// This spreads intakes evenly instead of front-loading them.
    /// </summary>
    private static void PlaceIntakesPaced(List<PlannedEvent> plan, PlanningContext ctx)
    {
        if (ctx.IntakeProducts.Count == 0)
            return;

        double smallestPortion = ctx.IntakeProducts.Min(p => p.CarbsG);
        for (int w = 0; w < ctx.FuelWindows.Count; w++)
        {
            var window = ctx.FuelWindows[w];
            double windowAim = ctx.CarbAim * window.CarbShare;
            double budget = windowAim - ExpectedSipCarbs(window, ctx, windowAim) - NonSipCarbsIn(plan, ctx, w);
            if (budget < SchedulingConstraints.MinRemainingCarbsForIntakeG)
                continue;

            double placed = 0;
            double span = window.LengthMin + 1;
            for (int t = window.StartMin; t <= window.EndMin; t++)
            {
                if (placed >= budget - SchedulingConstraints.MinRemainingCarbsForIntakeG)
                    break;

                double curve = budget * (t - window.StartMin + 1) / span;
                if (placed + smallestPortion * SchedulingConstraints.PacingMidpointFraction > curve)
                    continue;

                var product = ProductScorer.SelectBest(plan, t, ctx, ctx.CarbCap - NonSipCarbs(plan));
                if (product == null || placed + product.CarbsG * SchedulingConstraints.PacingMidpointFraction > curve)
                    continue;

                plan.Add(NutritionEventFactory.Intake(product, t, ctx.PhaseAt(t)));
                placed += product.CarbsG;
            }
        }
    }

    private static double ExpectedSipCarbs(FuelWindow window, PlanningContext ctx, double windowAim)
    {
        if (ctx.BackboneDrink is not { } drink)
            return 0;
        double capacity = SipTimes(window).Count() * NutritionEventFactory.SipCarbs(drink, SchedulingConstraints.SipVolumeMl);
        return Math.Min(windowAim * SchedulingConstraints.DrinkCarbShare, capacity);
    }

    // ─── Phase 3: Sip backbone ───────────────────────────────────────

    /// <summary>
    /// Replaces all sips with a backbone sized to close the gap between the discrete intakes and
    /// the carb aim. Volume per sip is clamped to [MinSipVolumeMl, SipVolumeMl]; when even minimum
    /// sips would overshoot, fewer (evenly spaced) sips are scheduled instead.
    /// </summary>
    private static void RebuildSipBackbone(List<PlannedEvent> plan, PlanningContext ctx)
    {
        plan.RemoveAll(e => e.IsSip);
        if (ctx.BackboneDrink is not { } drink)
            return;

        double need = ctx.CarbAim - TotalCarbs(plan);
        if (need <= 0)
            return;

        double maxPerSip = NutritionEventFactory.SipCarbs(drink, SchedulingConstraints.SipVolumeMl);
        var slots = ctx.FuelWindows.Select(w => SipTimes(w).ToList()).ToList();
        int totalSlots = slots.Sum(s => s.Count);
        if (totalSlots == 0 || maxPerSip <= 0)
            return;

        // Weight each window by its remaining deficit (falls back to slot count).
        var deficits = ctx.FuelWindows
            .Select((w, i) => Math.Max(0, ctx.CarbAim * w.CarbShare - NonSipCarbsIn(plan, ctx, i)))
            .ToList();
        double deficitSum = deficits.Sum();
        var allocation = slots
            .Select((s, i) => s.Count == 0 ? 0 : need * (deficitSum > 0 ? deficits[i] / deficitSum : (double)s.Count / totalSlots))
            .ToList();

        // Spill allocation that exceeds a window's sip capacity into windows with spare capacity.
        var capacity = slots.Select(s => s.Count * maxPerSip).ToList();
        double overflow = allocation.Select((a, i) => Math.Max(0, a - capacity[i])).Sum();
        for (int i = 0; i < allocation.Count; i++)
            allocation[i] = Math.Min(allocation[i], capacity[i]);
        double spare = allocation.Select((a, i) => capacity[i] - a).Sum();
        if (overflow > 0 && spare > 0)
        {
            double spillRatio = Math.Min(1, overflow / spare);
            for (int i = 0; i < allocation.Count; i++)
                allocation[i] += (capacity[i] - allocation[i]) * spillRatio;
        }

        for (int i = 0; i < slots.Count; i++)
            PlaceSips(plan, ctx, drink, slots[i], allocation[i]);
    }

    private static void PlaceSips(List<PlannedEvent> plan, PlanningContext ctx, ProductEnhanced drink, List<int> slots, double carbs)
    {
        if (carbs <= 0 || slots.Count == 0)
            return;

        double carbsPerMl = NutritionEventFactory.CarbsPerMl(drink);
        double minPerSip = carbsPerMl * SchedulingConstraints.MinSipVolumeMl;
        double perSip = carbs / slots.Count;

        IEnumerable<int> chosen = slots;
        if (perSip < minPerSip)
        {
            int count = (int)Math.Floor(carbs / minPerSip);
            if (count == 0)
                return;
            chosen = Enumerable.Range(0, count).Select(i => slots[(int)((i + 0.5) * slots.Count / count)]);
            perSip = carbs / count;
        }

        double sipMl = Math.Clamp(perSip / carbsPerMl, SchedulingConstraints.MinSipVolumeMl, SchedulingConstraints.SipVolumeMl);
        foreach (var t in chosen)
            plan.Add(NutritionEventFactory.Sip(drink, t, ctx.PhaseAt(t), sipMl));
    }

    private static IEnumerable<int> SipTimes(FuelWindow window)
    {
        for (int t = window.StartMin; t <= window.EndMin; t += SchedulingConstraints.SipIntervalMinutes)
            yield return t;
    }

    // ─── Phase 4: Deficit fill ───────────────────────────────────────

    /// <summary>
    /// Adds intakes only while sips at full volume still cannot reach the target. Each addition
    /// goes to the window with the largest deficit, at the legal minute farthest from other
    /// intakes, and never lifts discrete carbs above the cap.
    /// </summary>
    private static void FillDeficit(List<PlannedEvent> plan, PlanningContext ctx)
    {
        if (ctx.IntakeProducts.Count == 0)
            return;

        double smallestPortion = ctx.IntakeProducts.Min(p => p.CarbsG);
        // Each iteration adds one intake, so the number of race minutes bounds the loop.
        for (int guard = 0; guard <= ctx.DurationMinutes; guard++)
        {
            if (TotalCarbs(plan) >= ctx.Targets.CarbsG - CarbComparisonSlackG)
                return;

            double headroom = ctx.CarbCap - NonSipCarbs(plan);
            if (headroom < smallestPortion)
                return;

            var placement = FindSpreadPlacement(plan, ctx, headroom);
            if (placement != null)
                plan.Add(placement);
            else if (!TryUpgradeIntake(plan, ctx, headroom))
                return;

            RebuildSipBackbone(plan, ctx);
        }
    }

    /// <summary>
    /// When no legal minute is left for another intake (spacing / per-hour limits saturated), swap an
    /// existing intake for a larger product at the same minute. Picks the smallest upgrade that closes
    /// the deficit, otherwise the largest available one. Every swap is re-checked by the placement policy.
    /// </summary>
    private static bool TryUpgradeIntake(List<PlannedEvent> plan, PlanningContext ctx, double headroom)
    {
        double deficit = ctx.Targets.CarbsG - TotalCarbs(plan);
        (PlannedEvent Event, ProductEnhanced Product, double Delta)? best = null;

        foreach (var evt in plan.Where(e => e.Kind == EventKind.Intake).ToList())
        {
            foreach (var product in ctx.IntakeProducts)
            {
                double delta = product.CarbsG - evt.Carbs;
                if (delta <= 0 || delta > headroom || !PlacementPolicy.CanPlace(plan, evt.TimeMin, product, ctx, ignore: evt))
                    continue;

                bool better = best is not { } current
                    || (delta >= deficit && (current.Delta < deficit || delta < current.Delta))
                    || (delta < deficit && current.Delta < deficit && delta > current.Delta);
                if (better)
                    best = (evt, product, delta);
            }
        }

        if (best is not { } chosen)
            return false;

        plan[plan.IndexOf(chosen.Event)] = NutritionEventFactory.Intake(chosen.Product, chosen.Event.TimeMin, chosen.Event.Event.Phase);
        return true;
    }

    private static PlannedEvent? FindSpreadPlacement(List<PlannedEvent> plan, PlanningContext ctx, double headroom)
    {
        var windowOrder = ctx.FuelWindows
            .Select((w, i) => (Window: w, Deficit: ctx.CarbAim * w.CarbShare - CarbsIn(plan, ctx, i)))
            .OrderByDescending(x => x.Deficit)
            .Select(x => x.Window)
            .ToList();

        var intakeTimes = plan.Where(e => !e.IsSip).Select(e => e.TimeMin).ToList();
        foreach (var window in windowOrder)
        {
            foreach (var t in SpreadOrder(window, intakeTimes, window.StartMin))
            {
                var product = ProductScorer.SelectBest(plan, t, ctx, headroom);
                if (product != null)
                    return NutritionEventFactory.Intake(product, t, ctx.PhaseAt(t));
            }
        }
        return null;
    }

    /// <summary>Window minutes ordered by distance to the nearest intake (window edges count), later first on ties.</summary>
    private static List<int> SpreadOrder(FuelWindow window, IReadOnlyList<int> intakeTimes, int fromMin)
    {
        int start = Math.Max(fromMin, window.StartMin);
        if (start > window.EndMin)
            return new List<int>();

        return Enumerable.Range(start, window.EndMin - start + 1)
            .Select(t => (Time: t, Gap: Math.Min(
                Math.Min(t - window.StartMin, window.EndMin - t),
                intakeTimes.Count == 0 ? int.MaxValue : intakeTimes.Min(x => Math.Abs(x - t)))))
            .OrderByDescending(x => x.Gap)
            .ThenByDescending(x => x.Time)
            .Select(x => x.Time)
            .ToList();
    }

    // ─── Phase 5: Overshoot trim ─────────────────────────────────────

    /// <summary>
    /// Defensive: if the plan is still above the cap, remove intakes from the most densely loaded
    /// hour (earliest on ties, i.e. the front of the race) - never by default from the race end.
    /// </summary>
    private static void TrimOvershoot(List<PlannedEvent> plan, PlanningContext ctx)
    {
        double floor = ctx.Targets.CarbsG - ctx.CarbTolerance;
        while (TotalCarbs(plan) > ctx.CarbCap + CarbComparisonSlackG)
        {
            var intakes = plan.Where(e => e.Kind == EventKind.Intake).ToList();
            if (intakes.Count == 0)
                return;

            int densestStart = intakes
                .Select(e => e.TimeMin)
                .OrderByDescending(s => plan.Where(o => o.TimeMin >= s && o.TimeMin < s + OneHourMin).Sum(o => o.Carbs))
                .ThenBy(s => s)
                .First();

            double total = TotalCarbs(plan);
            var victim = intakes
                .Where(e => e.TimeMin >= densestStart && e.TimeMin < densestStart + OneHourMin)
                .Where(e => total - e.Carbs >= floor)
                .OrderByDescending(e => e.Carbs)
                .FirstOrDefault();
            if (victim == null)
                return;

            plan.Remove(victim);
            RebuildSipBackbone(plan, ctx);
        }
    }

    // ─── Phase 6: Coverage ───────────────────────────────────────────

    /// <summary>
    /// Ensures the last in-race event is at or after MinCoverageRatio of the race. Moving the latest
    /// intake later is preferred; a new intake is only added when moving is impossible and the cap allows.
    /// </summary>
    private static void EnsureCoverage(List<PlannedEvent> plan, PlanningContext ctx)
    {
        var inRace = plan.Where(e => e.TimeMin > 0).ToList();
        if (inRace.Count == 0 || inRace.Max(e => e.TimeMin) >= ctx.MinLastEventMin)
            return;

        int from = Math.Min(ctx.MinLastEventMin, ctx.LatestEventMin);
        var latest = plan.Where(e => e.Kind == EventKind.Intake).MaxBy(e => e.TimeMin);
        if (latest != null)
        {
            for (int t = from; t <= ctx.LatestEventMin; t++)
            {
                if (!PlacementPolicy.CanPlace(plan, t, latest.Product, ctx, ignore: latest))
                    continue;
                plan[plan.IndexOf(latest)] = NutritionEventFactory.MoveTo(latest, t, ctx.PhaseAt(t));
                RebuildSipBackbone(plan, ctx);
                return;
            }
        }

        double headroom = ctx.CarbCap - NonSipCarbs(plan);
        for (int t = from; t <= ctx.LatestEventMin; t++)
        {
            var product = ProductScorer.SelectBest(plan, t, ctx, headroom);
            if (product == null)
                continue;
            plan.Add(NutritionEventFactory.Intake(product, t, ctx.PhaseAt(t)));
            RebuildSipBackbone(plan, ctx);
            return;
        }
    }

    // ─── Phase 7: Front-load guard (final pass) ──────────────────────

    /// <summary>
    /// Enforces "no more than MaxFrontLoadFraction of carbs in the first FrontLoadWindowFraction of the race"
    /// on the final plan. Front-window intakes are moved later (same policy as every other placement);
    /// only if nothing can move are front-window sips, then intakes, dropped.
    /// </summary>
    internal static void EnforceFrontLoadConstraint(List<PlannedEvent> plan, PlanningContext ctx)
    {
        int windowEnd = ctx.FrontLoadWindowEndMin;
        bool IsFront(PlannedEvent e) => e.TimeMin >= 0 && e.TimeMin <= windowEnd;
        bool Violated()
        {
            double total = TotalCarbs(plan);
            return total > 0 && plan.Where(IsFront).Sum(e => e.Carbs) > total * SchedulingConstraints.MaxFrontLoadFraction;
        }

        while (Violated())
        {
            var candidates = plan.Where(e => e.Kind == EventKind.Intake && IsFront(e))
                .OrderByDescending(e => e.Carbs).ThenBy(e => e.TimeMin).ToList();

            bool moved = false;
            foreach (var evt in candidates)
            {
                if (FindLaterSlot(plan, evt, ctx, windowEnd + 1) is not int t)
                    continue;
                plan[plan.IndexOf(evt)] = NutritionEventFactory.MoveTo(evt, t, ctx.PhaseAt(t));
                moved = true;
                break;
            }
            if (!moved)
                break;
        }

        // Last resort - keeps the hard rule even when nothing can be moved.
        foreach (var evt in plan.Where(e => e.IsSip && IsFront(e)).OrderBy(e => e.TimeMin).ToList())
        {
            if (!Violated()) return;
            plan.Remove(evt);
        }
        foreach (var evt in plan.Where(e => !e.IsSip && IsFront(e)).OrderByDescending(e => e.Carbs).ToList())
        {
            if (!Violated()) return;
            plan.Remove(evt);
        }
    }

    private static int? FindLaterSlot(List<PlannedEvent> plan, PlannedEvent evt, PlanningContext ctx, int fromMin)
    {
        var intakeTimes = plan.Where(e => !e.IsSip && !ReferenceEquals(e, evt)).Select(e => e.TimeMin).ToList();
        var ownWindow = ctx.WindowAt(evt.TimeMin);
        var windows = ctx.FuelWindows.OrderBy(w => w == ownWindow ? 0 : 1).ToList();

        foreach (var window in windows)
        {
            foreach (var t in SpreadOrder(window, intakeTimes, fromMin))
                if (PlacementPolicy.CanPlace(plan, t, evt.Product, ctx, ignore: evt))
                    return t;
        }
        return null;
    }

    // ─── Finalisation ────────────────────────────────────────────────

    /// <summary>Sorts the plan and computes cumulative totals exactly once.</summary>
    private static List<NutritionEvent> Finalize(List<PlannedEvent> plan)
    {
        double carbs = 0;
        double caffeine = 0;
        var result = new List<NutritionEvent>(plan.Count);
        foreach (var planned in plan.OrderBy(e => e.TimeMin).ThenBy(e => e.Kind))
        {
            carbs += planned.Carbs;
            caffeine += planned.CaffeineMg;
            result.Add(planned.Event with
            {
                TotalCarbsSoFar = Math.Round(carbs, 1),
                TotalCaffeineSoFar = Math.Round(caffeine, 1)
            });
        }
        return result;
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static double TotalCarbs(IEnumerable<PlannedEvent> plan) => plan.Sum(e => e.Carbs);

    private static double NonSipCarbs(IEnumerable<PlannedEvent> plan) => plan.Where(e => !e.IsSip).Sum(e => e.Carbs);

    /// <summary>Carbs of discrete intakes attributed to fuel window <paramref name="index"/> (pre-race counts toward the first).</summary>
    private static double NonSipCarbsIn(IEnumerable<PlannedEvent> plan, PlanningContext ctx, int index) =>
        CarbsIn(plan.Where(e => !e.IsSip), ctx, index);

    private static double CarbsIn(IEnumerable<PlannedEvent> plan, PlanningContext ctx, int index)
    {
        var window = ctx.FuelWindows[index];
        return plan.Where(e => window.Contains(e.TimeMin) || (index == 0 && e.TimeMin < window.StartMin))
                   .Sum(e => e.Carbs);
    }

    /// <summary>
    /// Resolve the triathlon leg model (lenient) and collect structured warnings (leg-sum normalisation,
    /// or legs supplied for a non-triathlon sport). Returns a null model for single-phase sports.
    /// </summary>
    private static (ResolvedLegModel? Model, List<string> Warnings) ResolveLegModel(RaceProfile race)
    {
        if (race.SportType != SportType.Triathlon)
        {
            var warnings = race.Legs != null || race.Preset.HasValue
                ? new List<string>
                {
                    $"Triathlon leg inputs were provided for a {race.SportType} plan and were ignored " +
                    "(single-phase sports have no swim/bike/run legs)."
                }
                : new List<string>();
            return (null, warnings);
        }

        var model = TriathlonLegModel.Resolve(race.DurationHours, race.Legs, race.Preset, strict: false, out var legWarnings);
        return (model, legWarnings);
    }
}
