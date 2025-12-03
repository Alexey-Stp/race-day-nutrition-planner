namespace RaceDay.Core;

/// <summary>
/// Base exception for Race Day Nutrition Planner specific errors
/// </summary>
public class RaceDayException : Exception
{
    public RaceDayException() { }
    public RaceDayException(string message) : base(message) { }
    public RaceDayException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when required product types are missing
/// </summary>
public class MissingProductException : RaceDayException
{
    public string ProductType { get; }

    public MissingProductException(string productType)
        : base($"Required product type '{productType}' not found in product list")
    {
        ProductType = productType;
    }
}

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
