namespace RaceDay.Core.Utilities;
using RaceDay.Core.Models;

/// <summary>
/// Extension methods for nutrition plans
/// </summary>
public static class PlanExtensions
{
    /// <summary>
    /// Get a summary of the nutrition plan - aggregated shopping list with quantities and nutrition info
    /// </summary>
    /// <param name="plan">The nutrition plan</param>
    /// <returns>Shopping list summary with totals</returns>
    public static PlanSummary GetSummary(this RaceNutritionPlan plan)
    {
        var productSummaries = plan.ProductSummaries
            .Select(ps => new ShoppingItem(
                ProductName: ps.ProductName,
                TotalPortions: ps.TotalPortions
            ))
            .ToList();

        return new PlanSummary(
            ActivityName: plan.Race.SportType.ToString(),
            DurationHours: plan.Race.DurationHours,
            Temperature: plan.Race.Temperature,
            IntensityLevel: plan.Race.Intensity,
            NutritionTargets: plan.Targets,
            TotalNutrition: new NutritionTotals(
                CarbsG: plan.TotalCarbsG,
                FluidsMl: plan.TotalFluidsMl,
                SodiumMg: plan.TotalSodiumMg
            ),
            ShoppingList: productSummaries,
            ScheduleCount: plan.Schedule.Count
        );
    }
}

/// <summary>
/// Summary of a nutrition plan with aggregated data
/// </summary>
/// <param name="ActivityName">Type of activity</param>
/// <param name="DurationHours">Total duration in hours</param>
/// <param name="TemperatureC">Ambient temperature</param>
/// <param name="IntensityLevel">Exercise intensity</param>
/// <param name="NutritionTargets">Hourly nutrition targets</param>
/// <param name="TotalNutrition">Total nutrition consumed</param>
/// <param name="ShoppingList">Items needed for the race</param>
/// <param name="ScheduleCount">Number of intake points in the schedule</param>
public record PlanSummary(
    string ActivityName,
    double DurationHours,
    TemperatureCondition Temperature,
    IntensityLevel IntensityLevel,
    NutritionTargets NutritionTargets,
    NutritionTotals TotalNutrition,
    List<ShoppingItem> ShoppingList,
    int ScheduleCount
);

/// <summary>
/// Single item in the shopping list
/// </summary>
/// <param name="ProductName">Name of the product</param>
/// <param name="TotalPortions">Total number of portions needed</param>
public record ShoppingItem(
    string ProductName,
    double TotalPortions
);

/// <summary>
/// Total nutrition consumed during the race
/// </summary>
/// <param name="CarbsG">Total carbohydrates in grams</param>
/// <param name="FluidsMl">Total fluids in milliliters</param>
/// <param name="SodiumMg">Total sodium in milligrams</param>
public record NutritionTotals(
    double CarbsG,
    double FluidsMl,
    double SodiumMg
);
