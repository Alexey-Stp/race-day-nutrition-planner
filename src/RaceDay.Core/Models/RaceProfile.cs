namespace RaceDay.Core.Models;

/// <summary>
/// Race or training session characteristics
/// </summary>
/// <param name="SportType">Type of sport</param>
/// <param name="DurationHours">Duration in hours</param>
/// <param name="Temperature">Temperature condition (Cold, Moderate, Hot)</param>
/// <param name="Intensity">Exercise intensity level</param>
/// <param name="Legs">Optional explicit swim/bike/run leg durations (triathlon only)</param>
/// <param name="Preset">Optional distance preset providing default leg fractions (triathlon only)</param>
public record RaceProfile(
    SportType SportType,
    double DurationHours,
    TemperatureCondition Temperature,
    IntensityLevel Intensity,
    TriathlonLegs? Legs = null,
    DistancePreset? Preset = null
);
