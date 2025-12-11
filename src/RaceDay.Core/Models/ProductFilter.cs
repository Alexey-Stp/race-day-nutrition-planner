namespace RaceDay.Core.Models;

/// <summary>
/// Product filter for plan generation
/// </summary>
/// <param name="Brand">Filter by brand (e.g., "SiS", "Maurten"). Null = all brands</param>
/// <param name="ExcludeTypes">Exclude product types (e.g., ["gel", "drink"]). Empty = none excluded. Note: When used with Run sport in the API, "drink" and "recovery" types are automatically added to exclusions.</param>
public record ProductFilter(
    string? Brand = null,
    List<string>? ExcludeTypes = null
);
