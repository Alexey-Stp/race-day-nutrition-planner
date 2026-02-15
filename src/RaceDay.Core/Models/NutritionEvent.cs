namespace RaceDay.Core.Models;

/// <summary>
/// Nutrition event with timing and phase information
/// </summary>
/// <param name="TimeMin">Time point in minutes from race start</param>
/// <param name="Phase">Race phase (Swim, Bike, Run)</param>
/// <param name="PhaseDescription">User-friendly phase description</param>
/// <param name="ProductName">Name of the product to consume</param>
/// <param name="AmountPortions">Number of portions to consume (fractional for sips)</param>
/// <param name="Action">Action description (e.g., "Squeeze", "Sip", "Chew")</param>
/// <param name="TotalCarbsSoFar">Cumulative carbs consumed up to this point</param>
/// <param name="HasCaffeine">Whether this product contains caffeine</param>
/// <param name="CaffeineMg">Amount of caffeine in milligrams</param>
/// <param name="TotalCaffeineSoFar">Cumulative caffeine consumed up to this point</param>
/// <param name="CarbsInEvent">Carbs contributed by this specific event</param>
/// <param name="SipMl">Milliliters consumed in this sip event (null for non-drink events)</param>
public record NutritionEvent(
    int TimeMin,
    RacePhase Phase,
    string PhaseDescription,
    string ProductName,
    double AmountPortions,
    string Action,
    double TotalCarbsSoFar,
    bool HasCaffeine = false,
    double? CaffeineMg = null,
    double TotalCaffeineSoFar = 0,
    double CarbsInEvent = 0,
    double? SipMl = null
);
