namespace RaceDay.Core.Exceptions;

/// <summary>
/// Thrown when an embedded product or activity catalogue cannot be loaded or parsed.
/// </summary>
public class CatalogueLoadException : RaceDayException
{
    public string CatalogueName { get; }

    public CatalogueLoadException(string catalogueName, string message)
        : base($"Failed to load {catalogueName} catalogue: {message}")
    {
        CatalogueName = catalogueName;
    }

    public CatalogueLoadException(string catalogueName, string message, Exception inner)
        : base($"Failed to load {catalogueName} catalogue: {message}", inner)
    {
        CatalogueName = catalogueName;
    }
}
