namespace RaceDay.Core.Services;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;

/// <summary>
/// Service for generating advanced nutrition plans with sport-specific logic
/// </summary>
public class NutritionPlanService : INutritionPlanService
{
    private readonly AdvancedPlanGenerator _planGenerator = new();

    /// <summary>
    /// Generates an advanced nutrition plan with sport-specific optimization
    /// </summary>
    public List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 22)
    {
        // Convert Product to ProductEnhanced for advanced planning
        var enhancedProducts = products.Select(p => new ProductEnhanced(
            Name: p.Name,
            CarbsG: p.CarbsG,
            Texture: DetermineTexture(p.ProductType),
            HasCaffeine: p.CaffeineMg.HasValue && p.CaffeineMg > 0,
            CaffeineMg: p.CaffeineMg ?? 0,
            VolumeMl: p.VolumeMl,
            ProductType: p.ProductType
        )).ToList();

        return _planGenerator.GeneratePlan(race, athlete, enhancedProducts, intervalMin);
    }

    /// <summary>
    /// Determine product texture from product type
    /// </summary>
    private static ProductTexture DetermineTexture(string productType) =>
        productType.ToLower() switch
        {
            "gel" => ProductTexture.Gel,
            "drink" => ProductTexture.Drink,
            "energy drink" => ProductTexture.Drink,
            "bar" or "bake" or "energy bar" => ProductTexture.Bake,
            "chew" or "chews" => ProductTexture.Chew,
            _ => ProductTexture.Gel
        };
}
