namespace RaceDay.Core.Models;

/// <summary>
/// Nutrition product details
/// </summary>
/// <param name="Name">Product name</param>
/// <param name="ProductType">Type: "gel", "drink", or "bar"</param>
/// <param name="CarbsG">Carbohydrate content in grams</param>
/// <param name="SodiumMg">Sodium content in milligrams</param>
/// <param name="VolumeMl">Volume in milliliters (for drinks)</param>
public record Product(
    string Name,
    string ProductType,
    double CarbsG,
    double SodiumMg,
    double VolumeMl = 0
);
