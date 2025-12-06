namespace RaceDay.Core.Models;

/// <summary>
/// Temperature conditions affecting nutrition needs
/// </summary>
public enum TemperatureCondition
{
    /// <summary>Very Cold: ≤ 5°C - Reduces fluid needs, no sodium effect</summary>
    Cold,
    /// <summary>Moderate: 5-25°C - Baseline nutrition targets (no adjustments)</summary>
    Moderate,
    /// <summary>Hot: ≥ 25°C - Increases fluid and sodium needs significantly</summary>
    Hot
}
