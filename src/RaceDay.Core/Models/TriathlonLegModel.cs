namespace RaceDay.Core.Models;

using RaceDay.Core.Constants;
using RaceDay.Core.Exceptions;

/// <summary>
/// Explicit swim/bike/run leg durations (hours) supplied on a request.
/// When present, these are authoritative for both phase boundaries and
/// duration-proportional sodium/fluid distribution.
/// </summary>
public sealed record TriathlonLegs(double SwimH, double BikeH, double RunH)
{
    /// <summary>Sum of the three legs in hours.</summary>
    public double SumH => SwimH + BikeH + RunH;
}

/// <summary>
/// Named triathlon distance presets carrying default leg fractions.
/// </summary>
public enum DistancePreset
{
    Sprint,
    Olympic,

    /// <summary>Half-Ironman / 70.3.</summary>
    HalfIron,
    Ironman
}

/// <summary>
/// The single resolved leg model consumed by both <c>PlanGenerator</c> and
/// <c>NutritionCalculator</c>. No other source of triathlon leg boundaries or
/// per-leg carb ratios is permitted — see <see cref="TriathlonLegModel.Resolve"/>.
/// </summary>
/// <param name="SwimH">Swim leg duration in hours.</param>
/// <param name="BikeH">Bike leg duration in hours.</param>
/// <param name="RunH">Run leg duration in hours.</param>
/// <param name="BikeCarbRatio">Fraction of total carbs allocated to the bike leg.</param>
/// <param name="RunCarbRatio">Fraction of total carbs allocated to the run leg.</param>
public sealed record ResolvedLegModel(
    double SwimH,
    double BikeH,
    double RunH,
    double BikeCarbRatio,
    double RunCarbRatio)
{
    /// <summary>End of the swim leg (hours from start). Swim spans 0..SwimEndH.</summary>
    public double SwimEndH => SwimH;

    /// <summary>End of the bike leg (hours from start). Bike spans SwimEndH..BikeEndH.</summary>
    public double BikeEndH => SwimH + BikeH;

    /// <summary>End of the run leg (hours from start) — equals total duration.</summary>
    public double RunEndH => SwimH + BikeH + RunH;
}

/// <summary>
/// Resolves triathlon leg boundaries and per-leg carb ratios from an optional
/// explicit leg split, an optional distance preset, or (for legacy callers) a
/// named default split. This is the sole authority for triathlon leg data.
/// </summary>
public static class TriathlonLegModel
{
    // ─── Default split (legacy fallback) ──────────────────────────────
    // Preserved as named constants so the legacy path is byte-for-byte
    // identical to the historical inline 20/50/30 literals it replaces.
    public const double DefaultSwimFraction = 0.20;
    public const double DefaultBikeFraction = 0.50;
    public const double DefaultRunFraction = 0.30;

    // ─── Per-leg carb ratios (single source) ──────────────────────────
    // Bike carries the bulk of carbs (most digestible leg); swim carries none.
    public const double BikeCarbRatio = SchedulingConstraints.TriathlonBikeCarbRatio; // 0.70
    public const double RunCarbRatio = SchedulingConstraints.TriathlonRunCarbRatio;   // 0.30

    /// <summary>
    /// Tolerance (hours) within which an explicit leg sum is accepted as matching
    /// the total duration without normalisation.
    /// </summary>
    public const double SumToleranceHours = 0.05;

    /// <summary>Default leg fractions per distance preset (swim / bike / run).</summary>
    private static (double Swim, double Bike, double Run) PresetFractions(DistancePreset preset) =>
        preset switch
        {
            DistancePreset.Sprint => (0.17, 0.54, 0.29),
            DistancePreset.Olympic => (0.15, 0.52, 0.33),
            DistancePreset.HalfIron => (0.10, 0.56, 0.34),
            DistancePreset.Ironman => (0.09, 0.53, 0.38),
            _ => (DefaultSwimFraction, DefaultBikeFraction, DefaultRunFraction)
        };

    /// <summary>
    /// Resolve the authoritative leg model for a triathlon.
    ///
    /// Resolution policy:
    /// <list type="bullet">
    /// <item>Explicit <paramref name="legs"/> → used verbatim (subject to the sum-tolerance rule below).</item>
    /// <item><paramref name="preset"/> only → preset fractions expanded against <paramref name="durationHours"/>.</item>
    /// <item>Neither → default 20/50/30 split (legacy path, unchanged behaviour).</item>
    /// </list>
    /// When explicit legs are provided but their sum differs from <paramref name="durationHours"/>
    /// beyond <see cref="SumToleranceHours"/>: in lenient mode the legs are normalised
    /// proportionally and a structured warning is emitted; in <paramref name="strict"/> mode a
    /// <see cref="ValidationException"/> is thrown.
    /// </summary>
    public static ResolvedLegModel Resolve(
        double durationHours,
        TriathlonLegs? legs,
        DistancePreset? preset,
        bool strict,
        out List<string> warnings)
    {
        warnings = new List<string>();

        if (durationHours <= 0)
            throw new ValidationException(nameof(durationHours), "Duration must be greater than 0");

        if (legs != null)
            return ResolveFromExplicitLegs(durationHours, legs, strict, warnings);

        var fractions = preset.HasValue
            ? PresetFractions(preset.Value)
            : (DefaultSwimFraction, DefaultBikeFraction, DefaultRunFraction);

        return FromFractions(durationHours, fractions);
    }

    private static ResolvedLegModel ResolveFromExplicitLegs(
        double durationHours,
        TriathlonLegs legs,
        bool strict,
        List<string> warnings)
    {
        if (legs.SwimH < 0 || legs.BikeH < 0 || legs.RunH < 0)
            throw new ValidationException(nameof(legs), "Leg durations cannot be negative");

        double sum = legs.SumH;
        if (sum <= 0)
            throw new ValidationException(nameof(legs), "Leg durations must sum to more than 0");

        if (Math.Abs(sum - durationHours) <= SumToleranceHours)
            return new ResolvedLegModel(legs.SwimH, legs.BikeH, legs.RunH, BikeCarbRatio, RunCarbRatio);

        // Sum diverges from total duration.
        if (strict)
            throw new ValidationException(
                nameof(legs),
                $"Leg durations sum to {sum:0.##}h but DurationHours is {durationHours:0.##}h");

        // Lenient: normalise proportionally to the declared total and warn loudly.
        double scale = durationHours / sum;
        warnings.Add(
            $"Triathlon legs (swim {legs.SwimH:0.##}h, bike {legs.BikeH:0.##}h, run {legs.RunH:0.##}h) " +
            $"sum to {sum:0.##}h but DurationHours is {durationHours:0.##}h; " +
            $"legs normalised proportionally to {durationHours:0.##}h.");

        return new ResolvedLegModel(
            legs.SwimH * scale,
            legs.BikeH * scale,
            legs.RunH * scale,
            BikeCarbRatio,
            RunCarbRatio);
    }

    private static ResolvedLegModel FromFractions(
        double durationHours,
        (double Swim, double Bike, double Run) fractions) =>
        new(
            durationHours * fractions.Swim,
            durationHours * fractions.Bike,
            durationHours * fractions.Run,
            BikeCarbRatio,
            RunCarbRatio);
}
