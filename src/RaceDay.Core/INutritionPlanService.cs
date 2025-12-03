namespace RaceDay.Core;

/// <summary>
/// Service interface for generating nutrition plans
/// </summary>
public interface INutritionPlanService
{
    /// <summary>
    /// Generates a complete nutrition plan
    /// </summary>
    /// <param name="race">Race profile</param>
    /// <param name="athlete">Athlete profile</param>
    /// <param name="products">Available products</param>
    /// <param name="intervalMin">Time interval in minutes</param>
    /// <returns>Complete nutrition plan</returns>
    RaceNutritionPlan GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 20);
}
