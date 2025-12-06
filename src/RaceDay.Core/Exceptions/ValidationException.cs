namespace RaceDay.Core.Exceptions;

/// <summary>
/// Exception thrown when input validation fails
/// </summary>
public class ValidationException : RaceDayException
{
    public string PropertyName { get; }

    public ValidationException(string propertyName, string message)
        : base($"Validation failed for {propertyName}: {message}")
    {
        PropertyName = propertyName;
    }
}
