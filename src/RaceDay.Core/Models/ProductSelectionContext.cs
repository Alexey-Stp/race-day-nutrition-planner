namespace RaceDay.Core.Models;

/// <summary>
/// Immutable snapshot of selection context at the moment a product is being scored.
/// Passed to DynamicSelectionStrategy so dynamic bonuses can be computed without
/// coupling to PlannerState internals.
/// Sentinel value -1 is used for time fields when no prior event exists.
/// </summary>
public record ProductSelectionContext(
    ProductEnhanced? LastNonSipProduct,
    int LastNonSipTimeMin,
    int LastDrinkTimeMin,
    int CurrentTimeMin,
    RacePhase CurrentPhase
);
