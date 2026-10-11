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
    /// <param name="caffeineEnabled">Whether caffeinated products may be scheduled</param>
    /// <returns>List of nutrition events with detailed timing and phase info</returns>
    /// <exception cref="RaceDay.Core.Exceptions.ValidationException">Invalid race, athlete or product input.</exception>
    List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        IReadOnlyList<Product> products,
        bool caffeineEnabled = false);

    /// <summary>
    /// Generates a plan together with validation diagnostics and the targets it was built against.
    /// </summary>
    /// <exception cref="RaceDay.Core.Exceptions.ValidationException">Invalid race, athlete or product input.</exception>
    PlanResult GeneratePlanWithDiagnostics(
        RaceProfile race,
        AthleteProfile athlete,
        IReadOnlyList<Product> products,
        bool caffeineEnabled = false);
}
