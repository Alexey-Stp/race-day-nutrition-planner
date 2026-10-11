namespace RaceDay.Core.Tests;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>
/// Independent oracle for the hard scheduling rules every generated plan must satisfy.
/// Deliberately re-derives the rules from the constants instead of calling the planner's
/// own placement policy, so a planner bug cannot hide behind a matching checker bug.
/// </summary>
internal static class PlanInvariants
{
    private const double CarbRoundingSlackG = 0.5;
    private const double CumulativeSlack = 0.25;

    public static int ToMinutes(double hours) => (int)Math.Round(hours * 60);

    public static bool IsNonSip(NutritionEvent e) => e.Kind != EventKind.Sip;

    public static int MinSpacing(ProductEnhanced a, ProductEnhanced b, RacePhase phaseA, RacePhase phaseB)
    {
        bool bike = phaseA == RacePhase.Bike && phaseB == RacePhase.Bike;
        static bool IsSolid(ProductEnhanced p) => p.Texture is ProductTexture.Bake or ProductTexture.Chew;
        static bool IsGel(ProductEnhanced p) => p.Texture is ProductTexture.Gel or ProductTexture.LightGel;

        int texture = IsSolid(a) || IsSolid(b)
            ? (bike ? SchedulingConstraints.MinSolidSpacingBike : SchedulingConstraints.MinSolidSpacingRun)
            : IsGel(a) || IsGel(b)
                ? (bike ? SchedulingConstraints.MinGelSpacingBike : SchedulingConstraints.MinGelSpacingRun)
                : SchedulingConstraints.MinDrinkSpacing;

        return a.HasCaffeine && b.HasCaffeine
            ? Math.Max(SchedulingConstraints.MinCaffeineSpacing, texture)
            : texture;
    }

    public static void AssertSipClassificationConsistent(IReadOnlyList<NutritionEvent> plan)
    {
        foreach (var e in plan)
        {
            bool isSipAction = e.Action == "Sip";
            (e.Kind == EventKind.Sip).ShouldBe(isSipAction, $"Event at {e.TimeMin}min: Kind={e.Kind} but Action={e.Action}");
            (e.SipMl != null).ShouldBe(isSipAction, $"Event at {e.TimeMin}min: SipMl={e.SipMl} but Action={e.Action}");
            if (isSipAction)
                (e.AmountPortions < 1.0).ShouldBeTrue($"Sip at {e.TimeMin}min has a whole portion ({e.AmountPortions})");
            (e.HasCaffeine == (e.CaffeineMg is > 0)).ShouldBeTrue(
                $"Event at {e.TimeMin}min: HasCaffeine={e.HasCaffeine} but CaffeineMg={e.CaffeineMg}");
        }
    }

    public static void AssertCaffeineFieldsMatchProducts(IReadOnlyList<NutritionEvent> plan, IReadOnlyList<ProductEnhanced> products)
    {
        foreach (var e in plan)
        {
            var product = products.First(p => p.Name == e.ProductName);
            if (!product.HasCaffeine)
            {
                e.HasCaffeine.ShouldBeFalse($"{e.ProductName} at {e.TimeMin}min is caffeine-free but flagged");
                continue;
            }

            e.HasCaffeine.ShouldBeTrue($"{e.ProductName} at {e.TimeMin}min contains caffeine but HasCaffeine=false");
            e.CaffeineMg.ShouldNotBeNull();
            e.CaffeineMg!.Value.ShouldBe(product.CaffeineMg * e.AmountPortions, 0.6);
        }
    }

    public static void AssertPairwiseSpacing(IReadOnlyList<NutritionEvent> plan, IReadOnlyList<ProductEnhanced> products)
    {
        var nonSip = plan.Where(IsNonSip).OrderBy(e => e.TimeMin).ToList();
        for (int i = 0; i < nonSip.Count; i++)
        for (int j = i + 1; j < nonSip.Count; j++)
        {
            var a = nonSip[i];
            var b = nonSip[j];
            var pa = products.First(p => p.Name == a.ProductName);
            var pb = products.First(p => p.Name == b.ProductName);
            int required = MinSpacing(pa, pb, a.Phase, b.Phase);
            int gap = b.TimeMin - a.TimeMin;
            (gap >= required).ShouldBeTrue(
                $"{a.ProductName}@{a.TimeMin} and {b.ProductName}@{b.TimeMin} are {gap}min apart (< {required}min)");
        }
    }

    public static void AssertMaxIntakesPerHour(IReadOnlyList<NutritionEvent> plan)
    {
        var times = plan.Where(IsNonSip).Select(e => e.TimeMin).OrderBy(t => t).ToList();
        foreach (var start in times)
        {
            int count = times.Count(t => t >= start && t < start + 60);
            (count <= SchedulingConstraints.MaxIntakesPerHour).ShouldBeTrue(
                $"{count} non-sip intakes in the 60min window starting at {start}min (max {SchedulingConstraints.MaxIntakesPerHour})");
        }
    }

    public static void AssertCaffeineRules(IReadOnlyList<NutritionEvent> plan, bool caffeineEnabled, double ceilingMg)
    {
        var caffeinated = plan.Where(e => e.HasCaffeine).OrderBy(e => e.TimeMin).ToList();
        double total = plan.Sum(e => e.CaffeineMg ?? 0);

        if (!caffeineEnabled)
        {
            caffeinated.ShouldBeEmpty("Caffeine is disabled but the plan contains caffeinated events");
            total.ShouldBe(0);
            return;
        }

        (total <= ceilingMg + CarbRoundingSlackG).ShouldBeTrue($"Total caffeine {total}mg exceeds ceiling {ceilingMg}mg");
        for (int i = 1; i < caffeinated.Count; i++)
        {
            int gap = caffeinated[i].TimeMin - caffeinated[i - 1].TimeMin;
            (gap >= SchedulingConstraints.MinCaffeineSpacing).ShouldBeTrue(
                $"Caffeine at {caffeinated[i - 1].TimeMin}min and {caffeinated[i].TimeMin}min only {gap}min apart");
        }
    }

    public static void AssertWithinRaceBounds(IReadOnlyList<NutritionEvent> plan, int durationMinutes)
    {
        foreach (var e in plan)
        {
            (e.TimeMin <= durationMinutes).ShouldBeTrue($"{e.ProductName} at {e.TimeMin}min is after the finish ({durationMinutes}min)");
            if (e.TimeMin < 0)
                e.Kind.ShouldBe(EventKind.PreRace, $"Only the pre-race event may be before the start ({e.ProductName}@{e.TimeMin})");
        }
    }

    public static void AssertFrontLoadRule(IReadOnlyList<NutritionEvent> plan, int durationMinutes)
    {
        double total = plan.Sum(e => e.CarbsInEvent);
        if (total <= 0) return;

        int windowEnd = (int)(durationMinutes * SchedulingConstraints.FrontLoadWindowFraction);
        double front = plan.Where(e => e.TimeMin >= 0 && e.TimeMin <= windowEnd).Sum(e => e.CarbsInEvent);
        (front <= total * SchedulingConstraints.MaxFrontLoadFraction + CarbRoundingSlackG).ShouldBeTrue(
            $"Front-loaded: {front:F1}g of {total:F1}g in the first {windowEnd}min " +
            $"(max {SchedulingConstraints.MaxFrontLoadFraction:P0})");
    }

    public static void AssertCumulativeTotalsConsistent(IReadOnlyList<NutritionEvent> plan)
    {
        double carbs = 0;
        double caffeine = 0;
        int previousTime = int.MinValue;
        foreach (var e in plan)
        {
            (e.TimeMin >= previousTime).ShouldBeTrue("Plan must be sorted by time");
            previousTime = e.TimeMin;
            carbs += e.CarbsInEvent;
            caffeine += e.CaffeineMg ?? 0;
            e.TotalCarbsSoFar.ShouldBe(carbs, CumulativeSlack);
            e.TotalCaffeineSoFar.ShouldBe(caffeine, CumulativeSlack);
        }
    }

    public static void AssertCarbsWithinTolerance(PlanResult result, double targetCarbs)
    {
        double total = result.Events.Sum(e => e.CarbsInEvent);
        double tolerance = targetCarbs * SchedulingConstraints.TargetTolerancePercent;

        (total <= targetCarbs + tolerance + CarbRoundingSlackG).ShouldBeTrue(
            $"Plan overdelivers: {total:F1}g > {targetCarbs:F1}g + {tolerance:F1}g tolerance");

        bool underdelivers = total < targetCarbs - tolerance - CarbRoundingSlackG;
        if (underdelivers)
        {
            // Only acceptable when the product set physically cannot reach the target under the
            // spacing / intake-per-hour rules - and then the plan must say so.
            result.Warnings.ShouldContain(w => w.Contains("underdelivers"),
                $"Plan underdelivers ({total:F1}g < {targetCarbs:F1}g) without warning");
        }
    }

    public static void AssertAllHardRules(
        PlanResult result,
        IReadOnlyList<ProductEnhanced> products,
        int durationMinutes,
        bool caffeineEnabled)
    {
        var plan = result.Events;
        var targets = result.Targets.ShouldNotBeNull();

        result.Errors.ShouldBeEmpty();
        AssertSipClassificationConsistent(plan);
        AssertCaffeineFieldsMatchProducts(plan, products);
        AssertWithinRaceBounds(plan, durationMinutes);
        AssertPairwiseSpacing(plan, products);
        AssertMaxIntakesPerHour(plan);
        AssertCaffeineRules(plan, caffeineEnabled, targets.CaffeineMg);
        AssertFrontLoadRule(plan, durationMinutes);
        AssertCumulativeTotalsConsistent(plan);
        AssertCarbsWithinTolerance(result, targets.CarbsG);
    }
}
