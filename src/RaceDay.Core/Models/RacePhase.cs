namespace RaceDay.Core.Models;

/// <summary>
/// Race phase for multi-sport events
/// </summary>
public enum RacePhase
{
    /// <summary>Swimming phase (triathlon)</summary>
    Swim,
    /// <summary>Cycling phase (triathlon or cycling race)</summary>
    Bike,
    /// <summary>Running phase (triathlon or running race)</summary>
    Run
}
