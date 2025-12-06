namespace RaceDay.Core.Models;

/// <summary>
/// Complete race nutrition plan
/// </summary>
/// <param name="Race">Race profile used for calculation</param>
/// <param name="Targets">Hourly nutrition targets</param>
/// <param name="Schedule">Time-based intake schedule</param>
/// <param name="TotalCarbsG">Total carbohydrates in grams</param>
/// <param name="TotalFluidsMl">Total fluids in milliliters</param>
/// <param name="TotalSodiumMg">Total sodium in milligrams</param>
/// <param name="ProductSummaries">Shopping list with total quantities needed per product</param>
public record RaceNutritionPlan(
    RaceProfile Race,
    NutritionTargets Targets,
    List<IntakeItem> Schedule,
    double TotalCarbsG,
    double TotalFluidsMl,
    double TotalSodiumMg,
    List<ProductSummary> ProductSummaries
);
