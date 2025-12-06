namespace RaceDay.Core.Models;

/// <summary>
/// Hourly nutrition targets
/// </summary>
/// <param name="CarbsGPerHour">Carbohydrate target in grams per hour</param>
/// <param name="FluidsMlPerHour">Fluid target in milliliters per hour</param>
/// <param name="SodiumMgPerHour">Sodium target in milligrams per hour</param>
public record NutritionTargets(
    double CarbsGPerHour,
    double FluidsMlPerHour,
    double SodiumMgPerHour
);
