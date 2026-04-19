namespace RaceDay.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;

/// <summary>
/// Service for generating advanced nutrition plans with sport-specific logic
/// </summary>
public class NutritionPlanService : INutritionPlanService
{
    private readonly PlanGenerator _planGenerator = new();
    private readonly ILogger<NutritionPlanService> _logger;

    public NutritionPlanService(ILogger<NutritionPlanService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates an advanced nutrition plan with sport-specific optimization
    /// </summary>
    public List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products)
    {
        var enhancedProducts = ConvertToEnhancedProducts(products, _logger);
        return _planGenerator.GeneratePlan(race, athlete, enhancedProducts);
    }

    /// <summary>
    /// Generates an advanced nutrition plan with diagnostics (warnings and errors)
    /// </summary>
    public PlanResult GeneratePlanWithDiagnostics(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        bool caffeineEnabled = false)
    {
        var enhancedProducts = ConvertToEnhancedProducts(products, _logger);
        return _planGenerator.GeneratePlanWithDiagnostics(race, athlete, enhancedProducts, caffeineEnabled);
    }

    private static List<ProductEnhanced> ConvertToEnhancedProducts(List<Product> products, ILogger logger) =>
        products.Select(p => new ProductEnhanced(
            Name: p.Name,
            CarbsG: p.CarbsG,
            Texture: DetermineTexture(p.ProductType, logger),
            HasCaffeine: p.CaffeineMg.HasValue && p.CaffeineMg > 0,
            CaffeineMg: p.CaffeineMg ?? 0,
            VolumeMl: p.VolumeMl,
            ProductType: p.ProductType,
            SodiumMg: p.SodiumMg
        )).ToList();

    /// <summary>
    /// Determine product texture from product type
    /// </summary>
    private static ProductTexture DetermineTexture(string productType, ILogger logger) =>
        productType.ToLower() switch
        {
            "gel" => ProductTexture.Gel,
            "drink" => ProductTexture.Drink,
            "energy drink" => ProductTexture.Drink,
            "bar" or "bake" or "energy bar" => ProductTexture.Bake,
            "chew" or "chews" => ProductTexture.Chew,
            _ => LogAndReturnDefaultTexture(productType, logger)
        };

    private static ProductTexture LogAndReturnDefaultTexture(string productType, ILogger logger)
    {
        logger.LogWarning("Unknown product type '{ProductType}' — defaulting to Gel texture", productType);
        return ProductTexture.Gel;
    }
}
