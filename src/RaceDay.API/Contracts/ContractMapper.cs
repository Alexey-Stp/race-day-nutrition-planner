namespace RaceDay.API.Contracts;

using RaceDay.Core.Models;
using RaceDay.Core.Utilities;

/// <summary>
/// Maps between API contracts and Core domain types. Pure mapping only — no domain rules.
/// </summary>
internal static class ContractMapper
{
    private static readonly RacePhase[] SegmentOrder = [RacePhase.Swim, RacePhase.Bike, RacePhase.Run];

    public static Product ToDomain(this ProductRequest p) =>
        new(p.Name, p.ProductType, p.CarbsG, p.SodiumMg, p.VolumeMl, p.CaffeineMg);

    public static ProductFilter ToDomain(this ProductFilterRequest f) => new(f.Brand, f.ExcludeTypes);

    public static TriathlonLegs ToDomain(this TriathlonLegsRequest l) => new(l.SwimH, l.BikeH, l.RunH);

    public static Product ToPlannerProduct(this ProductInfo p) =>
        new(p.Name, p.ProductType, p.CarbsG, p.SodiumMg, p.VolumeMl, p.CaffeineMg);

    public static ProductResponse ToResponse(this ProductInfo p) =>
        new(p.Id, p.Brand, p.Name, p.ProductType, p.CarbsG, p.SodiumMg, p.CaloriesKcal, p.VolumeMl, p.ImageUrl, p.CaffeineMg);

    public static ActivityResponse ToResponse(this ActivityInfo a) =>
        new(a.Id, a.Name, a.SportType, a.Description, a.MinDurationHours, a.MaxDurationHours, a.BestTimeHours,
            a.Preset,
            a.LegFractions is { } f ? new LegFractionsResponse(f.Swim, f.Bike, f.Run) : null);

    public static AdvancedPlanResponse ToResponse(
        this PlanResult plan, RaceProfile race, AthleteProfile athlete, MultiNutrientTargets targets, bool caffeineEnabled)
    {
        var shopping = plan.Events.CalculateShoppingList();
        return new AdvancedPlanResponse(
            Race: new RaceResponse(
                race.SportType, race.DurationHours, race.Temperature, race.Intensity,
                race.Legs is { } l ? new TriathlonLegsResponse(l.SwimH, l.BikeH, l.RunH, l.SumH) : null,
                race.Preset),
            Athlete: new AthleteResponse(athlete.WeightKg),
            NutritionSchedule: plan.Events.Select(ToResponse).ToList(),
            ShoppingSummary: new ShoppingSummaryResponse(
                shopping.Items.Select(i => new ShoppingItemResponse(i.ProductName, i.TotalPortions, i.TotalCarbs)).ToList(),
                shopping.TotalProductCount,
                shopping.TotalCarbs),
            Warnings: plan.Warnings,
            Errors: plan.Errors,
            SegmentTargets: MapSegmentTargets(targets.SegmentTargets),
            Targets: targets.ToPlanTargetsResponse(),
            CaffeineEnabled: caffeineEnabled);
    }

    private static NutritionEventResponse ToResponse(NutritionEvent e) =>
        new(e.TimeMin, e.Phase, e.PhaseDescription, e.ProductName, e.AmountPortions, e.Action, e.TotalCarbsSoFar,
            e.HasCaffeine, e.CaffeineMg, e.TotalCaffeineSoFar, e.CarbsInEvent, e.SipMl, e.Kind);

    public static NutritionTargetsResponse ToTargetsResponse(this MultiNutrientTargets t) =>
        new(t.CarbsPerHour, t.FluidPerHour, t.SodiumPerHour, t.CarbsG, t.FluidMl, t.SodiumMg);

    public static PlanTargetsResponse ToPlanTargetsResponse(this MultiNutrientTargets t) =>
        new(t.CarbsPerHour, t.FluidPerHour, t.SodiumPerHour, t.CarbsG, t.FluidMl, t.SodiumMg, t.CaffeineMg);

    /// <summary>Flatten per-phase targets into a leg-ordered list (null for non-triathlon plans).</summary>
    private static List<SegmentTargetResponse>? MapSegmentTargets(IReadOnlyDictionary<RacePhase, PhaseTargets>? segmentTargets) =>
        segmentTargets?
            .Where(kv => SegmentOrder.Contains(kv.Key))
            .OrderBy(kv => Array.IndexOf(SegmentOrder, kv.Key))
            .Select(kv => new SegmentTargetResponse(
                kv.Key.ToString(), kv.Value.CarbsG, kv.Value.SodiumMg, kv.Value.FluidMl, kv.Value.DurationMinutes))
            .ToList();
}
