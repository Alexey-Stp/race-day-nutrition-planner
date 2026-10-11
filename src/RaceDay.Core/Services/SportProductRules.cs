namespace RaceDay.Core.Services;

using RaceDay.Core.Models;

/// <summary>
/// Sport-specific rules for which catalogue product types are offered to the planner.
/// </summary>
public static class SportProductRules
{
    /// <summary>
    /// Product types excluded for running: runners typically don't carry bottles, and recovery
    /// products are post-exercise only.
    /// </summary>
    public static readonly IReadOnlyList<string> RunExcludedProductTypes = ["drink", "recovery"];

    /// <summary>Product types that are always excluded for the given sport.</summary>
    public static IReadOnlyList<string> ExcludedProductTypesFor(SportType sportType) =>
        sportType == SportType.Run ? RunExcludedProductTypes : [];

    /// <summary>
    /// Returns <paramref name="filter"/> with the sport's mandatory exclusions merged into
    /// <see cref="ProductFilter.ExcludeTypes"/> (case-insensitive, no duplicates).
    /// </summary>
    public static ProductFilter ApplySportExclusions(ProductFilter? filter, SportType sportType)
    {
        filter ??= new ProductFilter();
        var mandatory = ExcludedProductTypesFor(sportType);
        if (mandatory.Count == 0)
            return filter;

        var excludeTypes = filter.ExcludeTypes?.ToList() ?? [];
        excludeTypes.AddRange(mandatory.Where(type =>
            !excludeTypes.Any(t => string.Equals(t?.Trim(), type, StringComparison.OrdinalIgnoreCase))));

        return filter with { ExcludeTypes = excludeTypes };
    }
}
