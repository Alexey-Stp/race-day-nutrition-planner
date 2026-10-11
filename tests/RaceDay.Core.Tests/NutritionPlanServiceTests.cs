namespace RaceDay.Core.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.Core.Constants;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;
using RaceDay.Core.Services;

public class NutritionPlanServiceTests
{
    private static readonly AthleteProfile Athlete = new(WeightKg: 70);
    private static readonly RaceProfile Race = new(SportType.Bike, 3, TemperatureCondition.Moderate, IntensityLevel.Hard);
    private readonly NutritionPlanService _service = new(NullLogger<NutritionPlanService>.Instance);

    [Theory]
    [InlineData("gel", 40, ProductTexture.Gel)]
    [InlineData("GEL", 40, ProductTexture.Gel)]
    [InlineData("gel", 22, ProductTexture.LightGel)]
    [InlineData("gel", 20, ProductTexture.LightGel)]
    [InlineData("drink", 40, ProductTexture.Drink)]
    [InlineData("Energy Drink", 40, ProductTexture.Drink)]
    [InlineData("bar", 40, ProductTexture.Bake)]
    [InlineData("Bake", 40, ProductTexture.Bake)]
    [InlineData("energy bar", 40, ProductTexture.Bake)]
    [InlineData("chew", 24, ProductTexture.Chew)]
    [InlineData("Chews", 24, ProductTexture.Chew)]
    public void DetermineTexture_KnownType_MapsCaseInsensitively(string productType, double carbs, ProductTexture expected)
    {
        NutritionPlanService.DetermineTexture(productType, carbs).ShouldBe(expected);
    }

    [Fact]
    public void DetermineTexture_GelAtLightGelThreshold_IsLightGel()
    {
        NutritionPlanService.DetermineTexture("gel", ProductEffectivenessProfiles.LightGelMaxCarbsG).ShouldBe(ProductTexture.LightGel);
        NutritionPlanService.DetermineTexture("gel", ProductEffectivenessProfiles.LightGelMaxCarbsG + 0.1).ShouldBe(ProductTexture.Gel);
    }

    [Fact]
    public void DetermineTexture_TurkishCulture_StillMapsUppercaseTypes()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            // Under tr-TR, "DRINK".ToLower() is "drınk" (dotless ı); mapping must be culture-invariant.
            NutritionPlanService.DetermineTexture("DRINK", 40).ShouldBe(ProductTexture.Drink);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void GeneratePlan_UnknownProductType_ThrowsValidationException()
    {
        var products = new List<Product> { new("Mystery", "powder", 30, 100) };

        var ex = Should.Throw<ValidationException>(() => _service.GeneratePlan(Race, Athlete, products));

        ex.PropertyName.ShouldBe(nameof(Product.ProductType));
        ex.Message.ShouldContain("powder");
    }

    [Fact]
    public void GeneratePlan_RecoveryProduct_IsExcludedFromInRacePlanning()
    {
        // Decision: "recovery" drinks (e.g. SiS REGO) are post-exercise products and are dropped
        // before planning rather than being scheduled as an in-race drink.
        var products = new List<Product>
        {
            new("SiS REGO Rapid Recovery Drink (500 ml)", "recovery", 50, 400, 500),
            new("Gel", "gel", 25, 50)
        };

        var plan = _service.GeneratePlan(Race, Athlete, products);

        plan.ShouldNotBeEmpty();
        plan.ShouldNotContain(e => e.ProductName.Contains("Recovery"));
        NutritionPlanService.NonRaceProductTypes.ShouldContain("RECOVERY");
    }

    [Fact]
    public async Task GeneratePlan_EveryCatalogueProductType_IsMappedOrExcluded()
    {
        var catalogue = await new ProductRepository().GetAllProductsAsync();
        var products = catalogue.Select(p => new Product(p.Name, p.ProductType, p.CarbsG, p.SodiumMg, p.VolumeMl, p.CaffeineMg)).ToList();

        foreach (var type in catalogue.Select(p => p.ProductType).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (NutritionPlanService.NonRaceProductTypes.Contains(type))
                continue;
            Should.NotThrow(() => NutritionPlanService.DetermineTexture(type, 30), $"Catalogue type '{type}' is not mapped");
        }

        Should.NotThrow(() => _service.GeneratePlanWithDiagnostics(Race, Athlete, products, caffeineEnabled: true));
    }

    [Fact]
    public void GeneratePlan_CaffeineEnabled_IsPassedThroughToPlanner()
    {
        var products = new List<Product>
        {
            new("Caf Gel", "gel", 25, 50, 0, CaffeineMg: 100),
            new("Gel", "gel", 40, 50)
        };

        var withCaffeine = _service.GeneratePlan(Race, Athlete, products, caffeineEnabled: true);
        var withoutCaffeine = _service.GeneratePlan(Race, Athlete, products, caffeineEnabled: false);

        withCaffeine.ShouldContain(e => e.HasCaffeine);
        withoutCaffeine.ShouldNotContain(e => e.HasCaffeine);
    }

    [Fact]
    public void GeneratePlanWithDiagnostics_ReturnsTargets()
    {
        var result = _service.GeneratePlanWithDiagnostics(Race, Athlete, new List<Product> { new("Gel", "gel", 40, 50) });

        result.Targets.ShouldNotBeNull();
        result.Targets.CarbsG.ShouldBe(NutritionConstants.Carbohydrates.HardIntensity * 3);
    }

    [Fact]
    public void GeneratePlan_InvalidAthlete_ThrowsValidationException()
    {
        var products = new List<Product> { new("Gel", "gel", 25, 50) };

        Should.Throw<ValidationException>(() => _service.GeneratePlan(Race, new AthleteProfile(double.NaN), products));
        Should.Throw<ValidationException>(() => _service.GeneratePlan(Race, new AthleteProfile(500), products));
    }

    [Fact]
    public void GeneratePlan_InvalidRace_ThrowsValidationException()
    {
        var products = new List<Product> { new("Gel", "gel", 25, 50) };
        var race = Race with { DurationHours = double.PositiveInfinity };

        Should.Throw<ValidationException>(() => _service.GeneratePlan(race, Athlete, products));
    }

    [Fact]
    public void GeneratePlan_InvalidProduct_ThrowsValidationException()
    {
        var products = new List<Product> { new("Gel", "gel", double.NaN, 50) };

        Should.Throw<ValidationException>(() => _service.GeneratePlan(Race, Athlete, products));
    }

    [Fact]
    public void GeneratePlan_TooManyProducts_ThrowsValidationException()
    {
        var products = Enumerable.Range(0, ValidationLimits.MaxProductsPerPlan + 1)
            .Select(i => new Product($"Gel {i}", "gel", 25, 50))
            .ToList();

        Should.Throw<ValidationException>(() => _service.GeneratePlan(Race, Athlete, products));
    }
}
