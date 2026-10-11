namespace RaceDay.Core.Models;

/// <summary>
/// Product filter for plan generation
/// </summary>
/// <param name="Brand">Filter by brand (e.g., "SiS", "Maurten"). Null = all brands</param>
/// <param name="ExcludeTypes">Exclude product types (e.g., ["gel", "drink"]). Empty = none excluded. Sport-mandatory exclusions (e.g. "drink" and "recovery" for Run) are merged in by <see cref="RaceDay.Core.Services.SportProductRules"/>.</param>
public record ProductFilter(
    string? Brand = null,
    IReadOnlyList<string>? ExcludeTypes = null
);
