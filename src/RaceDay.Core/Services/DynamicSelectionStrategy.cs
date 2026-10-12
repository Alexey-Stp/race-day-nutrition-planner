namespace RaceDay.Core.Services;

using System;
using RaceDay.Core.Constants;
using RaceDay.Core.Models;

/// <summary>
/// Context-aware scoring bonuses layered on top of the static score from ScoreProduct().
/// All methods are pure functions — no side effects, no state. Each factor is independently
/// callable so tests can verify individual bonuses in isolation.
/// All bonuses are bounded to prevent any single factor from dominating the static score.
/// </summary>
public static class DynamicSelectionStrategy
{
    // Caps keep the dynamic layer proportional to the static score range (~0–130 typical).
    private const double AlternationBonusCap     = 20.0;
    private const double DrinkTimerBonusCap      = 25.0;
    private const double EffectivenessGapBonusCap = 30.0;
    private const double PhaseChewBonusCap       = 15.0;

    // ── 1. Gel size alternation ───────────────────────────────────────────────

    /// <summary>
    /// Rewards alternating between large (≥35 g) and small (≤25 g) gels.
    /// Returns a positive bonus when the candidate continues the alternation pattern,
    /// a small penalty when it repeats the same size, or zero when either product
    /// is not a gel or no prior product exists.
    /// </summary>
    public static double GetAlternationBonus(ProductEnhanced candidate, ProductEnhanced? lastNonSip)
    {
        if (lastNonSip is null) return 0.0;

        bool candidateIsGel = candidate.Texture is ProductTexture.Gel or ProductTexture.LightGel;
        bool lastWasGel     = lastNonSip.Texture is ProductTexture.Gel or ProductTexture.LightGel;
        if (!candidateIsGel || !lastWasGel) return 0.0;

        bool lastLarge = lastNonSip.CarbsG  >= ProductEffectivenessProfiles.LargeGelThresholdG;
        bool lastSmall = lastNonSip.CarbsG  <= ProductEffectivenessProfiles.SmallGelThresholdG;
        bool candLarge = candidate.CarbsG   >= ProductEffectivenessProfiles.LargeGelThresholdG;
        bool candSmall = candidate.CarbsG   <= ProductEffectivenessProfiles.SmallGelThresholdG;

        if (lastLarge && candSmall) return  AlternationBonusCap;   // ✓ large → small
        if (lastSmall && candLarge) return  AlternationBonusCap;   // ✓ small → large
        if (lastLarge && candLarge) return -10.0;                  // ✗ same-size repeat
        if (lastSmall && candSmall) return  -5.0;                  // ✗ same-size repeat (milder)
        return 0.0;                                                 // medium-range — neutral
    }

    // ── 2. Drink hydration timer ──────────────────────────────────────────────

    /// <summary>
    /// Progressively increases drink priority as the gap since the last drink grows.
    /// No bonus below the threshold. Linear ramp between threshold and max-gap. Flat at cap beyond max-gap.
    /// Sentinel lastDrinkTimeMin = -1 means no drink ever consumed; treated as
    /// (currentTimeMin - threshold) so the ramp starts immediately at minute 0.
    /// Only applies to Drink texture candidates.
    /// </summary>
    public static double GetDrinkTimerBonus(ProductEnhanced candidate, int currentTimeMin, int lastDrinkTimeMin)
    {
        if (candidate.Texture != ProductTexture.Drink) return 0.0;

        int threshold    = ProductEffectivenessProfiles.DrinkTimerThresholdMin;
        int maxGap       = ProductEffectivenessProfiles.DrinkTimerMaxGapMin;
        int effectiveLast = lastDrinkTimeMin < 0 ? currentTimeMin - threshold : lastDrinkTimeMin;
        int minutesSince  = currentTimeMin - effectiveLast;

        if (minutesSince < threshold) return 0.0;

        double ratio = Math.Min((double)(minutesSince - threshold) / (maxGap - threshold), 1.0);
        return ratio * DrinkTimerBonusCap;
    }

    // ── 3. Effectiveness gap ─────────────────────────────────────────────────

    /// <summary>
    /// Boosts any non-drink product when the prior product's effectiveness window has expired.
    /// The bonus grows proportionally to the overrun beyond the midpoint window, capped at
    /// a 10-minute overrun. Drink candidates are excluded — their urgency is modelled by
    /// GetDrinkTimerBonus instead.
    /// </summary>
    public static double GetEffectivenessGapBonus(
        ProductEnhanced candidate,
        int currentTimeMin,
        int lastNonSipTimeMin,
        ProductTexture lastNonSipTexture,
        double lastNonSipCarbsG)
    {
        if (lastNonSipTimeMin < 0) return 0.0;
        if (candidate.Texture == ProductTexture.Drink) return 0.0;

        int midpoint = ProductEffectivenessProfiles.GetMidpointWindow(lastNonSipTexture, lastNonSipCarbsG);
        int overrun  = currentTimeMin - (lastNonSipTimeMin + midpoint);

        if (overrun <= 0) return 0.0;

        double ratio = Math.Min(overrun / 10.0, 1.0);
        return ratio * EffectivenessGapBonusCap;
    }

    // ── 4. Bike-phase chew bonus ──────────────────────────────────────────────

    /// <summary>
    /// Adds a dynamic bonus for chewable products on the Bike phase.
    /// Supplements the static GetSegmentSuitabilityScore (Chew: +15 on Bike) to
    /// reflect that chewing while cycling is ergonomically practical.
    /// Returns zero for all other textures and phases.
    /// </summary>
    public static double GetPhaseChewBonus(ProductEnhanced candidate, RacePhase phase)
    {
        if (candidate.Texture != ProductTexture.Chew) return 0.0;
        if (phase != RacePhase.Bike) return 0.0;
        return PhaseChewBonusCap;
    }

    // ── 5. Composite entry point ──────────────────────────────────────────────

    /// <summary>
    /// Sums all four dynamic bonuses for the given candidate and context.
    /// This is the single call site used by ScoreProduct().
    /// </summary>
    public static double CalculateTotalDynamicBonus(ProductEnhanced candidate, ProductSelectionContext ctx) =>
        GetAlternationBonus(candidate, ctx.LastNonSipProduct)
        + GetDrinkTimerBonus(candidate, ctx.CurrentTimeMin, ctx.LastDrinkTimeMin)
        + GetEffectivenessGapBonus(
            candidate,
            ctx.CurrentTimeMin,
            ctx.LastNonSipTimeMin,
            ctx.LastNonSipProduct?.Texture ?? ProductTexture.Gel,
            ctx.LastNonSipProduct?.CarbsG  ?? 0)
        + GetPhaseChewBonus(candidate, ctx.CurrentPhase);
}
