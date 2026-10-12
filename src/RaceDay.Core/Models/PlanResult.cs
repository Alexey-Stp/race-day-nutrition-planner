namespace RaceDay.Core.Models;

/// <summary>
/// Result of nutrition plan generation including the plan and validation diagnostics
/// </summary>
/// <param name="Events">Scheduled events ordered by time</param>
/// <param name="Warnings">Non-fatal plan quality diagnostics</param>
/// <param name="Errors">Constraint violations detected by validation</param>
/// <param name="Targets">The targets the plan was generated against (null only for legacy callers)</param>
public sealed record PlanResult(
    IReadOnlyList<NutritionEvent> Events,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    MultiNutrientTargets? Targets = null
);
