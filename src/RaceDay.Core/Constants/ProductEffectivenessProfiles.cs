namespace RaceDay.Core.Constants;

using RaceDay.Core.Models;

/// <summary>
/// Sports-science-derived effectiveness windows for nutrition products.
/// "Effectiveness window" = duration (minutes) over which a product actively sustains
/// blood-glucose contribution, based on gastric emptying rate and absorption kinetics.
/// Sources: Jeukendrup (2014), Burke &amp; Desbrow (2015), Stellingwerff (2011).
/// </summary>
public static class ProductEffectivenessProfiles
{
    // ── Effectiveness window ranges (minutes) per product category ───────────

    public const int DrinkSipMinMin    = 10;  // isotonic, rapid gastric transit
    public const int DrinkSipMaxMin    = 15;

    public const int LightGelMinMin    = 20;  // ≤22 g carbs, fast absorption
    public const int LightGelMaxMin    = 25;

    public const int StandardGelMinMin = 25;  // 23–34 g carbs
    public const int StandardGelMaxMin = 35;

    public const int DenseGelMinMin    = 35;  // ≥35 g carbs (Maurten GEL 160, SiS Beta Fuel)
    public const int DenseGelMaxMin    = 45;

    public const int ChewMinMin        = 25;  // mechanical digestion adds ~5 min vs equivalent gel
    public const int ChewMaxMin        = 35;

    public const int BakeMinMin        = 40;  // solid food, slowest gastric emptying
    public const int BakeMaxMin        = 55;

    // ── Carb thresholds for gel size classification ──────────────────────────

    public const double LightGelMaxCarbsG    = 22.0;  // ≤22 g → use LightGel window
    public const double StandardGelMaxCarbsG = 34.0;  // 23–34 g → use Standard window
    public const double LargeGelThresholdG   = 35.0;  // ≥35 g = "large" for alternation strategy
    public const double SmallGelThresholdG   = 25.0;  // ≤25 g = "small" for alternation strategy

    // ── Hydration urgency thresholds ─────────────────────────────────────────

    public const int DrinkTimerThresholdMin = 15;  // gap (min) that begins urgency ramp
    public const int DrinkTimerMaxGapMin    = 30;  // gap (min) at which urgency bonus is capped

    // ── Midpoint helper ──────────────────────────────────────────────────────

    /// Returns the midpoint of the effectiveness window (minutes).
    /// Used to estimate when the next product should ideally arrive.
    public static int GetMidpointWindow(ProductTexture texture, double carbsG) =>
        texture switch
        {
            ProductTexture.Drink    => (DrinkSipMinMin    + DrinkSipMaxMin)    / 2,
            ProductTexture.LightGel => (LightGelMinMin    + LightGelMaxMin)    / 2,
            ProductTexture.Chew     => (ChewMinMin        + ChewMaxMin)        / 2,
            ProductTexture.Bake     => (BakeMinMin        + BakeMaxMin)        / 2,
            ProductTexture.Gel when carbsG >= LargeGelThresholdG
                                    => (DenseGelMinMin    + DenseGelMaxMin)    / 2,
            _                       => (StandardGelMinMin + StandardGelMaxMin) / 2
        };
}
