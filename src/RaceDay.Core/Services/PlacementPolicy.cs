namespace RaceDay.Core.Services;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>
/// The single authority on whether a discrete intake may be scheduled at a given minute.
/// Every planning phase that inserts or moves an intake goes through <see cref="CanPlace"/>.
/// Sips form a backbone: they are exempt from pairwise spacing and the per-hour intake count,
/// but still respect race bounds and transition blackouts via the fuel windows.
/// </summary>
internal static class PlacementPolicy
{
    private const double CaffeineEpsilonMg = 0.01;
    private const int OneHourMin = 60;

    public static bool CanPlace(
        IReadOnlyList<PlannedEvent> plan,
        int timeMin,
        ProductEnhanced product,
        PlanningContext ctx,
        PlannedEvent? ignore = null)
    {
        // Race bounds, swim leg and the T2 blackout are all encoded in the fuel windows.
        if (ctx.WindowAt(timeMin) is null)
            return false;

        var phase = ctx.PhaseAt(timeMin);
        if (!IsTextureAllowed(product, phase) || !IsSolidTimingAllowed(product, timeMin, ctx))
            return false;

        if (product.HasCaffeine && !IsCaffeineAllowed(plan, timeMin, product, ctx, ignore))
            return false;

        foreach (var other in plan)
        {
            if (other.IsSip || ReferenceEquals(other, ignore))
                continue;
            int required = GetMinimumSpacing(other.Product, product, PlanningContext.SpacingPhase(other.Event.Phase, phase));
            if (Math.Abs(other.TimeMin - timeMin) < required)
                return false;
        }

        return !WouldExceedIntakesPerHour(plan, timeMin, ignore)
               && !WouldExceedConsecutiveSameProduct(plan, timeMin, product, ctx, ignore);
    }

    /// <summary>Sips only need to fall inside a fuel window.</summary>
    public static bool CanPlaceSip(int timeMin, PlanningContext ctx) => ctx.WindowAt(timeMin) is not null;

    /// <summary>
    /// Minimum minutes between two discrete intakes. Texture drives the base spacing (solid &gt; gel &gt; drink,
    /// bike more permissive than run); two caffeinated products additionally need the caffeine spacing.
    /// </summary>
    private static int TextureSpacing(ProductEnhanced product1, ProductEnhanced product2, bool bike)
    {
        if (IsSolid(product1) || IsSolid(product2))
            return bike ? SchedulingConstraints.MinSolidSpacingBike : SchedulingConstraints.MinSolidSpacingRun;
        if (IsGel(product1) || IsGel(product2))
            return bike ? SchedulingConstraints.MinGelSpacingBike : SchedulingConstraints.MinGelSpacingRun;
        return SchedulingConstraints.MinDrinkSpacing;
    }

    public static int GetMinimumSpacing(ProductEnhanced product1, ProductEnhanced product2, RacePhase phase)
    {
        int textureSpacing = TextureSpacing(product1, product2, phase == RacePhase.Bike);

        return product1.HasCaffeine && product2.HasCaffeine
            ? Math.Max(SchedulingConstraints.MinCaffeineSpacing, textureSpacing)
            : textureSpacing;
    }

    public static bool IsSolid(ProductEnhanced p) => p.Texture is ProductTexture.Bake or ProductTexture.Chew;

    public static bool IsGel(ProductEnhanced p) => p.Texture is ProductTexture.Gel or ProductTexture.LightGel;

    /// <summary>Bars are bike-only; everything else may be used in any fuelable phase.</summary>
    public static bool IsTextureAllowed(ProductEnhanced product, RacePhase phase) =>
        phase != RacePhase.Swim && (product.Texture != ProductTexture.Bake || phase == RacePhase.Bike);

    public static double RemainingCaffeineMg(IReadOnlyList<PlannedEvent> plan, PlanningContext ctx, PlannedEvent? ignore = null) =>
        ctx.CaffeineCeilingMg - plan.Where(e => !ReferenceEquals(e, ignore)).Sum(e => e.CaffeineMg);

    private static bool IsSolidTimingAllowed(ProductEnhanced product, int timeMin, PlanningContext ctx)
    {
        if (!IsSolid(product))
            return true;
        if (timeMin > ctx.DurationMinutes - SchedulingConstraints.NoSolidsBeforeFinish)
            return false;
        return ctx.T2Min is not int t2 || timeMin <= t2 - SchedulingConstraints.NoSolidsBeforeT2 || timeMin >= t2;
    }

    private static bool IsCaffeineAllowed(
        IReadOnlyList<PlannedEvent> plan, int timeMin, ProductEnhanced product, PlanningContext ctx, PlannedEvent? ignore)
    {
        if (!ctx.CaffeineEnabled || timeMin < ctx.CaffeineEarliestMin)
            return false;
        if (product.CaffeineMg > RemainingCaffeineMg(plan, ctx, ignore) + CaffeineEpsilonMg)
            return false;
        return !plan.Any(e => !ReferenceEquals(e, ignore) && e.CaffeineMg > 0
                              && Math.Abs(e.TimeMin - timeMin) < SchedulingConstraints.MinCaffeineSpacing);
    }

    /// <summary>
    /// True when adding an intake at <paramref name="timeMin"/> would put more than
    /// <see cref="SchedulingConstraints.MaxIntakesPerHour"/> non-sip intakes into any 60-minute window.
    /// </summary>
    private static bool WouldExceedIntakesPerHour(IReadOnlyList<PlannedEvent> plan, int timeMin, PlannedEvent? ignore)
    {
        var times = plan
            .Where(e => !e.IsSip && !ReferenceEquals(e, ignore) && Math.Abs(e.TimeMin - timeMin) < OneHourMin)
            .Select(e => e.TimeMin)
            .Append(timeMin)
            .ToList();

        // The busiest window containing timeMin starts at one of these event times.
        foreach (var start in times.Where(s => s <= timeMin))
        {
            int count = times.Count(t => t >= start && t < start + OneHourMin);
            if (count > SchedulingConstraints.MaxIntakesPerHour)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Enforces <see cref="SchedulingConstraints.MaxConsecutiveSameProduct"/> - but only while another
    /// product could legitimately be used at that time; with a single usable product the rule is moot.
    /// </summary>
    private static bool WouldExceedConsecutiveSameProduct(
        IReadOnlyList<PlannedEvent> plan, int timeMin, ProductEnhanced product, PlanningContext ctx, PlannedEvent? ignore)
    {
        var phase = ctx.PhaseAt(timeMin);
        double remainingCaffeine = RemainingCaffeineMg(plan, ctx, ignore);
        bool alternativeExists = ctx.IntakeProducts.Any(q =>
            q.Name != product.Name
            && IsTextureAllowed(q, phase)
            && (!q.HasCaffeine || (timeMin >= ctx.CaffeineEarliestMin && q.CaffeineMg <= remainingCaffeine)));
        if (!alternativeExists)
            return false;

        var others = plan
            .Where(e => !e.IsSip && !ReferenceEquals(e, ignore))
            .OrderBy(e => e.TimeMin)
            .ToList();

        // Length of the same-product run the candidate would join (before + candidate + after).
        int before = others.Where(e => e.TimeMin <= timeMin).Reverse()
            .TakeWhile(e => e.Product.Name == product.Name).Count();
        int after = others.Where(e => e.TimeMin > timeMin)
            .TakeWhile(e => e.Product.Name == product.Name).Count();
        return before + 1 + after > SchedulingConstraints.MaxConsecutiveSameProduct;
    }
}
