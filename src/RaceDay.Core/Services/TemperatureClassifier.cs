namespace RaceDay.Core.Services;

using RaceDay.Core.Constants;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;
using RaceDay.Core.Utilities;

/// <summary>
/// Maps ambient temperature to the <see cref="TemperatureCondition"/> the nutrition targets are based on.
/// </summary>
public static class TemperatureClassifier
{
    /// <summary>
    /// Classifies a temperature in °C using <see cref="SchedulingConstraints.ColdTemperatureThresholdC"/>
    /// and <see cref="SchedulingConstraints.HotTemperatureThresholdC"/> (both inclusive).
    /// </summary>
    /// <exception cref="ValidationException">The temperature is not finite or outside the accepted range.</exception>
    public static TemperatureCondition FromCelsius(double temperatureC)
    {
        Validation.ValidateTemperatureC(temperatureC);
        return temperatureC switch
        {
            <= SchedulingConstraints.ColdTemperatureThresholdC => TemperatureCondition.Cold,
            >= SchedulingConstraints.HotTemperatureThresholdC => TemperatureCondition.Hot,
            _ => TemperatureCondition.Moderate
        };
    }

    /// <summary>
    /// Resolves the condition from exactly one of a named condition or a temperature in °C.
    /// Supplying both, or neither, is rejected so the caller's intent is never ambiguous.
    /// </summary>
    /// <exception cref="ValidationException">Neither or both inputs were supplied, or the supplied one is invalid.</exception>
    public static TemperatureCondition Resolve(TemperatureCondition? condition, double? temperatureC) =>
        (condition, temperatureC) switch
        {
            ({ }, { }) => throw new ValidationException("Temperature",
                "Provide either 'temperature' or 'temperatureC', not both"),
            ({ } named, null) => Validation.ValidateDefined(named, "Temperature"),
            (null, { } celsius) => FromCelsius(celsius),
            _ => throw new ValidationException("Temperature",
                "Either 'temperature' (Cold, Moderate, Hot) or 'temperatureC' is required")
        };
}
