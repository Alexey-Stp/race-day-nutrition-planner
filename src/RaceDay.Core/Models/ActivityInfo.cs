namespace RaceDay.Core.Models;

/// <summary>
/// Activity information with time limits and best times
/// </summary>
/// <param name="Id">Unique activity identifier</param>
/// <param name="Name">Display name of the activity</param>
/// <param name="SportType">Associated sport type</param>
/// <param name="Description">Activity description</param>
/// <param name="MinDurationHours">Minimum recommended duration in hours</param>
/// <param name="MaxDurationHours">Maximum typical duration in hours</param>
/// <param name="BestTimeHours">Professional/elite time in hours</param>
/// <param name="Preset">Distance preset this activity maps to (triathlon presets only)</param>
/// <param name="LegFractions">Default swim/bike/run leg fractions (triathlon presets only)</param>
public record ActivityInfo(
    string Id,
    string Name,
    SportType SportType,
    string Description,
    double MinDurationHours,
    double MaxDurationHours,
    double BestTimeHours,
    DistancePreset? Preset = null,
    LegFractions? LegFractions = null
);

/// <summary>
/// Default swim/bike/run leg fractions for a triathlon distance preset.
/// Fractions are of total duration and should sum to ~1.0.
/// </summary>
public record LegFractions(
    double Swim,
    double Bike,
    double Run
);
