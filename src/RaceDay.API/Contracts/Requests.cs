namespace RaceDay.API.Contracts;

using RaceDay.Core.Models;

/// <summary>
/// Request for <c>POST /api/metadata/targets</c>.
/// </summary>
public record TargetsRequest(
    double AthleteWeightKg,
    SportType SportType,
    double DurationHours,
    TemperatureCondition Temperature,
    IntensityLevel Intensity
);

/// <summary>
/// Request for <c>POST /api/plan/generate</c>. Exactly one of <see cref="Temperature"/> or
/// <see cref="TemperatureC"/> is required; supplying both is rejected.
/// Either <see cref="Products"/> (non-empty) or <see cref="Filter"/> must be supplied.
/// </summary>
public record PlanGenerationRequest(
    double AthleteWeightKg,
    SportType SportType,
    double DurationHours,
    IntensityLevel Intensity,
    TemperatureCondition? Temperature = null,
    double? TemperatureC = null,
    List<ProductRequest>? Products = null,
    ProductFilterRequest? Filter = null,
    bool? CaffeineEnabled = null,
    TriathlonLegsRequest? Legs = null,
    DistancePreset? DistancePreset = null
);

/// <summary>
/// Catalogue filter. For Run, drink and recovery products are always excluded.
/// </summary>
public record ProductFilterRequest(
    string? Brand = null,
    IReadOnlyList<string>? ExcludeTypes = null
);

/// <summary>
/// Explicit swim/bike/run leg durations (hours) for a triathlon request.
/// </summary>
public record TriathlonLegsRequest(
    double SwimH,
    double BikeH,
    double RunH
);

/// <summary>
/// A product supplied explicitly on a plan request.
/// </summary>
public record ProductRequest(
    string Name,
    string ProductType,
    double CarbsG,
    double SodiumMg,
    double VolumeMl,
    double? CaffeineMg = null
);
