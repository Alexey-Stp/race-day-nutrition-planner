namespace RaceDay.Core.Services;
using RaceDay.Core.Models;

/// <summary>
/// Service interface for generating advanced nutrition plans
/// </summary>
public interface INutritionPlanService
{
    /// <summary>
    /// Generates an advanced nutrition plan with sport-specific logic
    /// </summary>
    /// <param name="race">Race profile</param>
    /// <param name="athlete">Athlete profile</param>
    /// <param name="products">Available products</param>
    /// <param name="intervalMin">Time interval in minutes (ignored for advanced planning)</param>
    /// <returns>List of nutrition events with detailed timing and phase info</returns>
    List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 22);
}
