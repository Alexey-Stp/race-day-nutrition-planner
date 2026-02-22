namespace RaceDay.Core.Models;

/// <summary>
/// Result of nutrition plan generation including the plan and validation diagnostics
/// </summary>
public sealed record PlanResult(
    List<NutritionEvent> Events,
    List<string> Warnings,
    List<string> Errors
);
