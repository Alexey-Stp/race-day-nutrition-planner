namespace RaceDay.Core.Constants;

/// <summary>
/// Single source of input limits, shared by Core validation and the API layer.
/// </summary>
public static class ValidationLimits
{
    public const double MinWeightKg = 1;
    public const double MaxWeightKg = 200;

    public const double MinDurationHours = 0.1;
    public const double MaxDurationHours = 24;

    public const double MinTemperatureC = -20;
    public const double MaxTemperatureC = 55;

    public const int MinIntervalMin = 1;
    public const int MaxIntervalMin = 120;

    /// <summary>Maximum number of products accepted for a single plan request.</summary>
    public const int MaxProductsPerPlan = 50;
}
