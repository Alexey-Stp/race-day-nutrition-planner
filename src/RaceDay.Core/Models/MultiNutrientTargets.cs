namespace RaceDay.Core.Models;

/// <summary>
/// Multi-nutrient targets for race nutrition planning
/// </summary>
public record MultiNutrientTargets(
    double CarbsG,
    double SodiumMg,
    double FluidMl,
    double CaffeineMg,
    double CarbsPerHour,
    double SodiumPerHour,
    double FluidPerHour,
    Dictionary<RacePhase, PhaseTargets>? SegmentTargets = null
);

/// <summary>
/// Phase-specific nutrition targets
/// </summary>
public record PhaseTargets(
    double CarbsG,
    double SodiumMg,
    double FluidMl,
    double DurationMinutes
);
