namespace RaceDay.Core.Services;

using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>A race phase expressed in whole minutes from the start.</summary>
internal sealed record PhaseSegment(RacePhase Phase, int StartMin, int EndMin);

/// <summary>
/// A time range in which fuelling is allowed, with the share of total carbs it should carry.
/// Triathlons have one window per fuelable leg (bike, run); other sports have one window.
/// </summary>
internal sealed record FuelWindow(RacePhase Phase, int StartMin, int EndMin, double CarbShare)
{
    public bool Contains(int timeMin) => timeMin >= StartMin && timeMin <= EndMin;
    public int LengthMin => EndMin - StartMin;
}

/// <summary>A scheduled event paired with the product it was built from.</summary>
internal sealed record PlannedEvent(NutritionEvent Event, ProductEnhanced Product)
{
    public int TimeMin => Event.TimeMin;
    public EventKind Kind => Event.Kind;
    public bool IsSip => Event.Kind == EventKind.Sip;
    public double Carbs => Event.CarbsInEvent;
    public double CaffeineMg => Event.CaffeineMg ?? 0;
}

/// <summary>
/// Immutable inputs for one planning run: race geometry (phases, fuel windows), targets,
/// and the product pools each planning phase may draw from.
/// </summary>
internal sealed class PlanningContext
{
    public PlanningContext(
        RaceProfile race,
        IReadOnlyList<ProductEnhanced> products,
        bool caffeineEnabled,
        ResolvedLegModel? legModel,
        MultiNutrientTargets targets)
    {
        DurationMinutes = PlanGenerator.ToMinutes(race.DurationHours);
        Targets = targets;
        CaffeineEnabled = caffeineEnabled;
        AllProducts = products;
        Phases = BuildPhases(race.SportType, DurationMinutes, legModel);
        IsTriathlon = race.SportType == SportType.Triathlon;
        T2Min = IsTriathlon ? Phases.First(p => p.Phase == RacePhase.Bike).EndMin : null;
        FuelWindows = BuildFuelWindows(legModel);

        // Caffeinated products are removed up-front when caffeine is disabled, so no phase can use them.
        UsableProducts = products
            .Where(p => p.CarbsG > 0)
            .Where(p => caffeineEnabled || !p.HasCaffeine)
            .ToList();

        var nonDrinks = UsableProducts.Where(p => p.Texture != ProductTexture.Drink).ToList();
        IntakeProducts = nonDrinks.Count > 0
            ? nonDrinks
            : UsableProducts.Where(p => p.Texture == ProductTexture.Drink).ToList();

        // The backbone is always caffeine-free so sips never interact with the caffeine ceiling.
        BackboneDrink = UsableProducts
            .Where(p => p.Texture == ProductTexture.Drink && !p.HasCaffeine
                        && p.CarbsG > SchedulingConstraints.BackboneDrinkMinCarbsG)
            .OrderByDescending(p => p.CarbsG)
            .FirstOrDefault();
    }

    public int DurationMinutes { get; }
    public MultiNutrientTargets Targets { get; }
    public bool CaffeineEnabled { get; }
    public IReadOnlyList<ProductEnhanced> AllProducts { get; }
    public IReadOnlyList<ProductEnhanced> UsableProducts { get; }
    public IReadOnlyList<ProductEnhanced> IntakeProducts { get; }
    public ProductEnhanced? BackboneDrink { get; }
    public IReadOnlyList<PhaseSegment> Phases { get; }
    public IReadOnlyList<FuelWindow> FuelWindows { get; }
    public bool IsTriathlon { get; }
    public int? T2Min { get; }

    public RacePhase MainPhase => Phases[0].Phase;
    public int LatestEventMin => Math.Max(0, DurationMinutes - SchedulingConstraints.RaceEndSafetyMarginMin);
    public int EarliestIntakeMin => Math.Min(SchedulingConstraints.EarliestInRaceIntakeMin, LatestEventMin);
    public int PreRaceTimeMin => -AdvancedNutritionConfig.PreRaceNutritionMinutesBefore;
    public int CaffeineEarliestMin =>
        (int)Math.Ceiling(DurationMinutes * SchedulingConstraints.CaffeinePreferredStartPercent);
    public int FrontLoadWindowEndMin => (int)(DurationMinutes * SchedulingConstraints.FrontLoadWindowFraction);
    public int MinLastEventMin => (int)(DurationMinutes * SchedulingConstraints.MinCoverageRatio);

    public double CarbTolerance => Targets.CarbsG * SchedulingConstraints.TargetTolerancePercent;
    public double CarbCap => Targets.CarbsG + CarbTolerance;
    public double CarbAim => Math.Min(CarbCap, Targets.CarbsG * (1 + SchedulingConstraints.TargetAimMarginPercent));
    public double CaffeineCeilingMg => CaffeineEnabled ? Targets.CaffeineMg : 0;

    public RacePhase PhaseAt(int timeMin)
    {
        if (timeMin < 0) return MainPhase;
        foreach (var segment in Phases)
            if (timeMin >= segment.StartMin && timeMin < segment.EndMin)
                return segment.Phase;
        return Phases[^1].Phase;
    }

    public FuelWindow? WindowAt(int timeMin) => FuelWindows.FirstOrDefault(w => w.Contains(timeMin));

    /// <summary>Phase used for pairwise spacing: bike values only apply when both events are on the bike.</summary>
    public static RacePhase SpacingPhase(RacePhase a, RacePhase b) =>
        a == RacePhase.Bike && b == RacePhase.Bike ? RacePhase.Bike : RacePhase.Run;

    private static IReadOnlyList<PhaseSegment> BuildPhases(SportType sport, int durationMinutes, ResolvedLegModel? legModel)
    {
        if (sport == SportType.Triathlon && legModel != null)
        {
            int swimEnd = PlanGenerator.ToMinutes(legModel.SwimEndH);
            int bikeEnd = PlanGenerator.ToMinutes(legModel.BikeEndH);
            return new List<PhaseSegment>
            {
                new(RacePhase.Swim, 0, swimEnd),
                new(RacePhase.Bike, swimEnd, bikeEnd),
                new(RacePhase.Run, bikeEnd, durationMinutes)
            };
        }

        var phase = sport == SportType.Bike ? RacePhase.Bike : RacePhase.Run;
        return new List<PhaseSegment> { new(phase, 0, durationMinutes) };
    }

    private IReadOnlyList<FuelWindow> BuildFuelWindows(ResolvedLegModel? legModel)
    {
        if (!IsTriathlon || legModel == null)
            return new List<FuelWindow> { new(MainPhase, EarliestIntakeMin, LatestEventMin, 1.0) };

        var bike = Phases.First(p => p.Phase == RacePhase.Bike);
        int bikeStart = bike.StartMin + SchedulingConstraints.NoFuelingAfterT1;
        int bikeEnd = bike.EndMin - AdvancedNutritionConfig.BikeToRunTransitionMarginMin;
        int runStart = bike.EndMin + SchedulingConstraints.NoFuelingAfterT2;

        var windows = new List<FuelWindow>();
        if (bikeEnd >= bikeStart)
            windows.Add(new FuelWindow(RacePhase.Bike, bikeStart, bikeEnd, legModel.BikeCarbRatio));
        if (LatestEventMin >= runStart)
            windows.Add(new FuelWindow(RacePhase.Run, runStart, LatestEventMin, legModel.RunCarbRatio));

        // Normalise shares so the windows that exist carry the whole target.
        double totalShare = windows.Sum(w => w.CarbShare);
        return totalShare > 0
            ? windows.Select(w => w with { CarbShare = w.CarbShare / totalShare }).ToList()
            : windows;
    }
}
