namespace RaceDay.Core;

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
