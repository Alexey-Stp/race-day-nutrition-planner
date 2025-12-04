namespace RaceDay.Core;

/// <summary>
/// Service for generating nutrition plans with validation and error handling
/// </summary>
public class NutritionPlanService : INutritionPlanService
{
    /// <summary>
    /// Generates a complete nutrition plan with validation
    /// </summary>
    public RaceNutritionPlan GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 20)
    {
        return PlanGenerator.Generate(race, athlete, products, intervalMin);
    }
}
