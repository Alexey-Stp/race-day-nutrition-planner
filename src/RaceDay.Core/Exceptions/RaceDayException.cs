namespace RaceDay.Core.Exceptions;

/// <summary>
/// Base exception for Race Day Nutrition Planner specific errors
/// </summary>
public class RaceDayException : Exception
{
    public RaceDayException() { }
    public RaceDayException(string message) : base(message) { }
    public RaceDayException(string message, Exception inner) : base(message, inner) { }
}
