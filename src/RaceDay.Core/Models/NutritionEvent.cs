namespace RaceDay.Core.Models;

/// <summary>
/// Nutrition event with timing and phase information
/// </summary>
/// <param name="TimeMin">Time point in minutes from race start</param>
/// <param name="Phase">Race phase (Swim, Bike, Run)</param>
/// <param name="ProductName">Name of the product to consume</param>
/// <param name="AmountPortions">Number of portions to consume</param>
/// <param name="Action">Action description (e.g., "Squeeze", "Drink", "Chew")</param>
/// <param name="TotalCarbsSoFar">Cumulative carbs consumed up to this point</param>
/// <param name="HasCaffeine">Whether this product contains caffeine</param>
public record NutritionEvent(
    int TimeMin,
    RacePhase Phase,
    string ProductName,
    double AmountPortions,
    string Action,
    double TotalCarbsSoFar,
    bool HasCaffeine = false,
    double? CaffeineMg = null
);
