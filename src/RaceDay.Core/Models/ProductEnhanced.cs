namespace RaceDay.Core.Models;

/// <summary>
/// Enhanced product information with texture and caffeine details
/// </summary>
/// <param name="Name">Product name</param>
/// <param name="CarbsG">Carbohydrate content in grams</param>
/// <param name="Texture">Product texture/format</param>
/// <param name="HasCaffeine">Whether product contains caffeine</param>
/// <param name="CaffeineMg">Caffeine content in milligrams (0 if no caffeine)</param>
/// <param name="VolumeMl">Volume in milliliters (for drinks)</param>
/// <param name="ProductType">Type category (e.g., "Electrolyte", "Energy")</param>
public record ProductEnhanced(
    string Name,
    double CarbsG,
    ProductTexture Texture,
    bool HasCaffeine,
    double CaffeineMg,
    double VolumeMl = 0,
    string ProductType = ""
);
