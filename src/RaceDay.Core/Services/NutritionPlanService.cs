namespace RaceDay.Core.Services;
using Microsoft.Extensions.Logging;
using RaceDay.Core.Constants;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;
using RaceDay.Core.Utilities;

/// <summary>
/// Service for generating advanced nutrition plans with sport-specific logic.
/// This is the validation boundary: every input is validated before reaching <see cref="PlanGenerator"/>.
/// </summary>
public class NutritionPlanService : INutritionPlanService
{
    /// <summary>
    /// Catalogue product types that are not used for in-race planning. Recovery drinks
    /// (protein + carbs, e.g. SiS REGO) are post-exercise products, so they are dropped
    /// rather than scheduled as a race drink.
    /// </summary>
    public static readonly IReadOnlySet<string> NonRaceProductTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "recovery" };

    private readonly PlanGenerator _planGenerator = new();
    private readonly ILogger<NutritionPlanService> _logger;

    public NutritionPlanService(ILogger<NutritionPlanService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        IReadOnlyList<Product> products,
        bool caffeineEnabled = false) =>
        GeneratePlanWithDiagnostics(race, athlete, products, caffeineEnabled).Events.ToList();

    /// <inheritdoc />
    public PlanResult GeneratePlanWithDiagnostics(
        RaceProfile race,
        AthleteProfile athlete,
        IReadOnlyList<Product> products,
        bool caffeineEnabled = false)
    {
        ValidateInputs(race, athlete, products);
        var enhancedProducts = ConvertToEnhancedProducts(products);
        if (enhancedProducts.Count < products.Count)
            _logger.LogInformation("Excluded {Count} non-race product(s) from planning", products.Count - enhancedProducts.Count);

        return _planGenerator.GeneratePlanWithDiagnostics(race, athlete, enhancedProducts, caffeineEnabled);
    }

    private static void ValidateInputs(RaceProfile race, AthleteProfile athlete, IReadOnlyList<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        Validation.ValidateRaceProfile(race);
        Validation.ValidateAthleteProfile(athlete);
        Validation.ValidateProductCount(products.Count);
        foreach (var product in products)
            Validation.ValidateProduct(product);
    }

    internal static List<ProductEnhanced> ConvertToEnhancedProducts(IEnumerable<Product> products) =>
        products
            .Where(p => !NonRaceProductTypes.Contains(p.ProductType.Trim()))
            .Select(p => new ProductEnhanced(
                Name: p.Name,
                CarbsG: p.CarbsG,
                Texture: DetermineTexture(p.ProductType, p.CarbsG),
                HasCaffeine: p.CaffeineMg is > 0,
                CaffeineMg: p.CaffeineMg ?? 0,
                VolumeMl: p.VolumeMl,
                ProductType: p.ProductType,
                SodiumMg: p.SodiumMg))
            .ToList();

    /// <summary>
    /// Maps a catalogue/request product type to a texture. Gels at or below the light-gel carb
    /// threshold become <see cref="ProductTexture.LightGel"/>. Unknown types are rejected rather than
    /// silently treated as gels.
    /// </summary>
    internal static ProductTexture DetermineTexture(string productType, double carbsG) =>
        productType.Trim().ToLowerInvariant() switch
        {
            "gel" when carbsG <= ProductEffectivenessProfiles.LightGelMaxCarbsG => ProductTexture.LightGel,
            "gel" => ProductTexture.Gel,
            "drink" or "energy drink" => ProductTexture.Drink,
            "bar" or "bake" or "energy bar" => ProductTexture.Bake,
            "chew" or "chews" => ProductTexture.Chew,
            _ => throw new ValidationException(nameof(Product.ProductType),
                $"Unknown product type '{productType}'. Supported: gel, drink, energy drink, bar, bake, energy bar, chew, chews, recovery")
        };
}
