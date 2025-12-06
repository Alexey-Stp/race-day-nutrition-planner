namespace RaceDay.Core.Models;

/// <summary>
/// Single intake item in the schedule
/// </summary>
/// <param name="TimeMin">Time point in minutes from start</param>
/// <param name="ProductName">Name of the product to consume</param>
/// <param name="AmountPortions">Number of portions to consume</param>
/// <param name="Product">Product details (for advanced planning)</param>
/// <param name="Phase">Race phase when this intake occurs</param>
/// <param name="TotalCarbsSoFar">Total carbohydrates consumed up to this point (in grams)</param>
public record IntakeItem(
    int TimeMin,
    string ProductName,
    double AmountPortions,
    Product? Product = null,
    RacePhase? Phase = null,
    double TotalCarbsSoFar = 0
);
