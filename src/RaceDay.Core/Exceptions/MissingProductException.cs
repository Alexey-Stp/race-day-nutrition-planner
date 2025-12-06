namespace RaceDay.Core.Exceptions;

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
