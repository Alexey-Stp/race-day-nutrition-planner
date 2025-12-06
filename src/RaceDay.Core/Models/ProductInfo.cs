namespace RaceDay.Core.Models;

/// <summary>
/// Detailed product information from the product catalog
/// </summary>
/// <param name="Id">Unique product identifier</param>
/// <param name="Brand">Product brand name</param>
/// <param name="Name">Product name</param>
/// <param name="ProductType">Type: "gel", "drink", or "bar"</param>
/// <param name="CarbsG">Carbohydrate content in grams</param>
/// <param name="SodiumMg">Sodium content in milligrams</param>
/// <param name="CaloriesKcal">Calorie content in kilocalories</param>
/// <param name="VolumeMl">Volume in milliliters (for drinks)</param>
/// <param name="ImageUrl">URL to product image</param>
public record ProductInfo(
    string Id,
    string Brand,
    string Name,
    string ProductType,
    double CarbsG,
    double SodiumMg,
    double CaloriesKcal,
    double VolumeMl,
    string ImageUrl
);
