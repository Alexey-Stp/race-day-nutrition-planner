namespace RaceDay.Core.Models;

/// <summary>
/// Product filter for plan generation
/// </summary>
/// <param name="Brand">Filter by brand (e.g., "SiS", "Maurten"). Null = all brands</param>
/// <param name="ExcludeTypes">Exclude product types (e.g., ["gel", "caffeine"]). Empty = none excluded</param>
public record ProductFilter(
    string? Brand = null,
    List<string>? ExcludeTypes = null
);
