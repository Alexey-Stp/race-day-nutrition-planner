namespace RaceDay.Core.Models;

/// <summary>
/// Race or training session characteristics
/// </summary>
/// <param name="SportType">Type of sport</param>
/// <param name="DurationHours">Duration in hours</param>
/// <param name="Temperature">Temperature condition (Cold, Moderate, Hot)</param>
/// <param name="Intensity">Exercise intensity level</param>
public record RaceProfile(
    SportType SportType,
    double DurationHours,
    TemperatureCondition Temperature,
    IntensityLevel Intensity
);
