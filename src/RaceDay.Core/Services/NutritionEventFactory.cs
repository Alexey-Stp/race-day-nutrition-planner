namespace RaceDay.Core.Services;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>
/// The only place a <see cref="NutritionEvent"/> is constructed by the planner. Every event gets
/// its Kind, Action, AmountPortions, SipMl and caffeine fields derived from the product in one way,
/// so classification (sip vs intake) and caffeine accounting can never drift apart.
/// Cumulative totals are filled in once, at the end of planning.
/// </summary>
internal static class NutritionEventFactory
{
    private const double MinPortion = 0.01;

    public static PlannedEvent PreRace(ProductEnhanced product, int timeMin, RacePhase phase) =>
        Whole(product, timeMin, phase, EventKind.PreRace);

    public static PlannedEvent Intake(ProductEnhanced product, int timeMin, RacePhase phase) =>
        Whole(product, timeMin, phase, EventKind.Intake);

    public static PlannedEvent Sip(ProductEnhanced drink, int timeMin, RacePhase phase, double sipMl)
    {
        double volume = DrinkVolume(drink);
        double ml = Math.Round(Math.Min(sipMl, volume));
        double fraction = ml / volume;
        double caffeine = drink.HasCaffeine ? Math.Round(drink.CaffeineMg * fraction, 1) : 0;

        var evt = new NutritionEvent(
            TimeMin: timeMin,
            Phase: phase,
            PhaseDescription: PhaseDescription(phase),
            ProductName: drink.Name,
            AmountPortions: Math.Max(MinPortion, Math.Round(fraction, 2)),
            Action: "Sip",
            TotalCarbsSoFar: 0,
            HasCaffeine: caffeine > 0,
            CaffeineMg: caffeine > 0 ? caffeine : null,
            TotalCaffeineSoFar: 0,
            CarbsInEvent: Math.Round(drink.CarbsG * fraction, 1),
            SipMl: ml,
            Kind: EventKind.Sip);
        return new PlannedEvent(evt, drink);
    }

    /// <summary>Rebuilds an event at a different time (used when moving events later).</summary>
    public static PlannedEvent MoveTo(PlannedEvent planned, int timeMin, RacePhase phase) =>
        planned.Kind == EventKind.Sip
            ? Sip(planned.Product, timeMin, phase, planned.Event.SipMl ?? SchedulingConstraints.SipVolumeMl)
            : Whole(planned.Product, timeMin, phase, planned.Kind);

    public static double DrinkVolume(ProductEnhanced drink) =>
        drink.VolumeMl > 0 ? drink.VolumeMl : SchedulingConstraints.DefaultDrinkVolumeMl;

    public static double CarbsPerMl(ProductEnhanced drink) => drink.CarbsG / DrinkVolume(drink);

    /// <summary>Carbs a sip of <paramref name="sipMl"/> delivers, after the same rounding the event uses.</summary>
    public static double SipCarbs(ProductEnhanced drink, double sipMl) =>
        Math.Round(drink.CarbsG * Math.Round(sipMl) / DrinkVolume(drink), 1);

    public static string PhaseDescription(RacePhase phase) =>
        phase switch
        {
            RacePhase.Swim => "Swim - Lower intensity nutrition due to difficulty of consuming during water",
            RacePhase.Bike => "Bike - Optimal for consuming nutrition, easier digestion",
            RacePhase.Run => "Run - Stomach more sensitive, prefer gels and drinks",
            _ => "Race Phase"
        };

    private static PlannedEvent Whole(ProductEnhanced product, int timeMin, RacePhase phase, EventKind kind)
    {
        var evt = new NutritionEvent(
            TimeMin: timeMin,
            Phase: phase,
            PhaseDescription: PhaseDescription(phase),
            ProductName: product.Name,
            AmountPortions: 1,
            Action: WholePortionAction(product.Texture),
            TotalCarbsSoFar: 0,
            HasCaffeine: product.HasCaffeine && product.CaffeineMg > 0,
            CaffeineMg: product.HasCaffeine && product.CaffeineMg > 0 ? product.CaffeineMg : null,
            TotalCaffeineSoFar: 0,
            CarbsInEvent: product.CarbsG,
            SipMl: null,
            Kind: kind);
        return new PlannedEvent(evt, product);
    }

    private static string WholePortionAction(ProductTexture texture) =>
        texture switch
        {
            ProductTexture.Gel or ProductTexture.LightGel => "Squeeze",
            ProductTexture.Drink => "Drink",
            ProductTexture.Chew => "Chew",
            ProductTexture.Bake => "Eat",
            _ => "Consume"
        };
}
