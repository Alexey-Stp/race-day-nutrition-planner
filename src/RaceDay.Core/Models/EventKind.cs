namespace RaceDay.Core.Models;

/// <summary>
/// Explicit classification of a scheduled nutrition event.
/// </summary>
public enum EventKind
{
    /// <summary>Intake before the start gun (negative TimeMin).</summary>
    PreRace,
    /// <summary>Partial drink from the sip backbone (SipMl is set, AmountPortions &lt; 1).</summary>
    Sip,
    /// <summary>Discrete in-race intake of a whole portion (gel, chew, bar or whole drink).</summary>
    Intake
}
