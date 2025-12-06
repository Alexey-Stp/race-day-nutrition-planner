namespace RaceDay.Core.Models;

/// <summary>
/// Product summary for shopping list
/// </summary>
/// <param name="ProductName">Name of the product</param>
/// <param name="TotalPortions">Total number of portions needed for the race</param>
public record ProductSummary(
    string ProductName,
    double TotalPortions
);
