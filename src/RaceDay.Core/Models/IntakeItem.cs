namespace RaceDay.Core.Models;

/// <summary>
/// Single intake item in the schedule
/// </summary>
/// <param name="TimeMin">Time point in minutes from start</param>
/// <param name="ProductName">Name of the product to consume</param>
/// <param name="AmountPortions">Number of portions to consume</param>
public record IntakeItem(
    int TimeMin,
    string ProductName,
    double AmountPortions
);
