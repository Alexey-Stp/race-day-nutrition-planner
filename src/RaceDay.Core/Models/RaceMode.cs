namespace RaceDay.Core.Models;

/// <summary>
/// Type of race or endurance activity
/// </summary>
public enum RaceMode
{
    /// <summary>Running only</summary>
    Running,
    /// <summary>Cycling only</summary>
    Cycling,
    /// <summary>Half triathlon (swim/bike/run)</summary>
    TriathlonHalf,
    /// <summary>Full triathlon (swim/bike/run)</summary>
    TriathlonFull
}
