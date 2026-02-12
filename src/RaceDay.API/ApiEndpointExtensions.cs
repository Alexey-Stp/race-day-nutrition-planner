using RaceDay.Core.Services;
using RaceDay.Core.Repositories;
using RaceDay.Core.Models;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Utilities;

namespace RaceDay.API;

/// <summary>
/// Extension methods for mapping API endpoints
/// </summary>
public static class ApiEndpointExtensions
{
    /// <summary>
    /// Map product-related API endpoints
    /// </summary>
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", GetAllProducts)
            .WithName("GetAllProducts")
            .WithDescription("Retrieve all available products");

        group.MapGet("/{id}", GetProductById)
            .WithName("GetProductById")
            .WithDescription("Get a specific product by ID");

        group.MapGet("/type/{type}", GetProductsByType)
            .WithName("GetProductsByType")
            .WithDescription("Filter products by type (gel, drink, bar)");

        group.MapGet("/search", SearchProducts)
            .WithName("SearchProducts")
            .WithDescription("Search products by name or brand");
    }

    /// <summary>
    /// Map activity-related API endpoints
    /// </summary>
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activities")
            .WithTags("Activities");

        group.MapGet("/", GetAllActivities)
            .WithName("GetAllActivities")
            .WithDescription("Retrieve all predefined activities");

        group.MapGet("/{id}", GetActivityById)
            .WithName("GetActivityById")
            .WithDescription("Get a specific activity by ID");

        group.MapGet("/type/{sportType}", GetActivitiesByType)
            .WithName("GetActivitiesByType")
            .WithDescription("Filter activities by sport type (Run, Bike, Triathlon)");

        group.MapGet("/search", SearchActivities)
            .WithName("SearchActivities")
            .WithDescription("Search activities by name or description");
    }

    /// <summary>
    /// Map nutrition plan generation API endpoints
    /// </summary>
    public static void MapPlanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/plan")
            .WithTags("Nutrition Plan");

        group.MapPost("/generate", GeneratePlan)
            .WithName("GeneratePlan")
            .WithDescription("Generate a nutrition plan based on race parameters, athlete profile, and available products");
    }

    /// <summary>
    /// Map UI metadata API endpoints (descriptions, ranges, etc. for frontend)
    /// </summary>
    public static void MapMetadataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/metadata")
            .WithTags("Metadata");

        group.MapGet("/", GetUIMetadata)
            .WithName("GetUIMetadata")
            .WithDescription("Get all UI metadata including temperature, intensity, and default activity information");

        group.MapGet("/temperatures", GetTemperatureMetadata)
            .WithName("GetTemperatureMetadata")
            .WithDescription("Get temperature condition metadata with ranges and effects");

        group.MapGet("/intensities", GetIntensityMetadata)
            .WithName("GetIntensityMetadata")
            .WithDescription("Get intensity level metadata with icons, carb ranges, and effects");

        group.MapGet("/defaults", GetDefaults)
            .WithName("GetDefaults")
            .WithDescription("Get default values for the application");

        group.MapGet("/configuration", GetConfigurationMetadata)
            .WithName("GetConfigurationMetadata")
            .WithDescription("Get all nutrition configuration including sport-specific parameters, thresholds, and descriptions");

        group.MapPost("/targets", CalculateNutritionTargets)
            .WithName("CalculateNutritionTargets")
            .WithDescription("Calculate nutrition targets for a specific athlete and race parameters");
    }

    // Product Handlers
    private static async Task<IResult> GetAllProducts(IProductRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var products = await repository.GetAllProductsAsync(cancellationToken);
            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading products: {ex.Message}");
        }
    }

    private static async Task<IResult> GetProductById(string id, IProductRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var product = await repository.GetProductByIdAsync(id, cancellationToken);
            return product == null ? Results.NotFound() : Results.Ok(product);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading product: {ex.Message}");
        }
    }

    private static async Task<IResult> GetProductsByType(string type, IProductRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var products = await repository.GetProductsByTypeAsync(type, cancellationToken);
            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading products: {ex.Message}");
        }
    }

    private static async Task<IResult> SearchProducts(string query, IProductRepository repository, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Results.BadRequest("Search query is required");

        try
        {
            var products = await repository.SearchProductsAsync(query, cancellationToken);
            return Results.Ok(products);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error searching products: {ex.Message}");
        }
    }

    // Activity Handlers
    private static async Task<IResult> GetAllActivities(CancellationToken cancellationToken)
    {
        try
        {
            var activities = await ActivityRepository.GetAllActivitiesAsync(cancellationToken);
            return Results.Ok(activities);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading activities: {ex.Message}");
        }
    }

    private static async Task<IResult> GetActivityById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var activity = await ActivityRepository.GetActivityByIdAsync(id, cancellationToken);
            return activity == null ? Results.NotFound() : Results.Ok(activity);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading activity: {ex.Message}");
        }
    }

    private static async Task<IResult> GetActivitiesByType(string sportType, CancellationToken cancellationToken)
    {
        try
        {
            if (!Enum.TryParse<SportType>(sportType, ignoreCase: true, out var parsedSportType))
                return Results.BadRequest("Invalid sport type. Valid values: Run, Bike, Triathlon");

            var activities = await ActivityRepository.GetActivitiesBySportTypeAsync(parsedSportType, cancellationToken);
            return Results.Ok(activities);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading activities: {ex.Message}");
        }
    }

    private static async Task<IResult> SearchActivities(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Results.BadRequest("Search query is required");

        try
        {
            var activities = await ActivityRepository.SearchActivitiesAsync(query, cancellationToken);
            return Results.Ok(activities);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error searching activities: {ex.Message}");
        }
    }

    // Plan Generation Handlers
    private static async Task<IResult> GeneratePlan(PlanGenerationRequest request, IProductRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (request == null)
                return Results.BadRequest("Request body is required");

            List<Product> products;

            // Get products either from explicit list or from filter
            if (request.Products != null && request.Products.Count > 0)
            {
                // Use provided products
                products = request.Products.Select(p => new Product(
                    p.Name,
                    p.ProductType,
                    p.CarbsG,
                    p.SodiumMg,
                    p.VolumeMl,
                    p.CaffeineMg
                )).ToList();
            }
            else if (request.Filter != null)
            {
                // Use filtered products from repository
                var filteredProductInfos = await repository.GetFilteredProductsAsync(request.Filter, cancellationToken);

                if (filteredProductInfos.Count == 0)
                    return Results.BadRequest("No products found matching the specified filter");

                products = filteredProductInfos.Select(p => new Product(
                    p.Name,
                    p.ProductType,
                    p.CarbsG,
                    p.SodiumMg,
                    p.VolumeMl,
                    p.CaffeineMg
                )).ToList();
            }
            else
            {
                return Results.BadRequest("Either 'products' or 'filter' must be provided");
            }

            if (products.Count == 0)
                return Results.BadRequest("At least one product is required");

            // Convert temperature Celsius to TemperatureCondition enum
            var temperatureCondition = request.TemperatureC switch
            {
                <= 5 => TemperatureCondition.Cold,
                >= 25 => TemperatureCondition.Hot,
                _ => TemperatureCondition.Moderate
            };

            // Create profiles
            var athlete = new AthleteProfile(request.AthleteWeightKg);
            var race = new RaceProfile(
                request.SportType,
                request.DurationHours,
                temperatureCondition,
                request.Intensity
            );

            // Generate advanced plan using the service
            var service = new NutritionPlanService();
            var planResult = service.GeneratePlanWithDiagnostics(race, athlete, products, caffeineEnabled: request.CaffeineEnabled ?? false);

            // Calculate shopping summary using extension
            var shoppingSummary = planResult.Events.CalculateShoppingList();

            // Create response with advanced plan data, diagnostics, and shopping summary
            var response = new AdvancedPlanResponse(
                race,
                athlete,
                planResult.Events,
                shoppingSummary,
                planResult.Warnings,
                planResult.Errors
            );

            return Results.Ok(response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (MissingProductException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating plan: {ex.Message}");
        }
    }

    // Metadata Handlers
    private static IResult GetUIMetadata()
    {
        try
        {
            var metadata = UIMetadataService.GetUIMetadata();
            return Results.Ok(metadata);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading metadata: {ex.Message}");
        }
    }

    private static IResult GetTemperatureMetadata()
    {
        try
        {
            var metadata = UIMetadataService.GetTemperatureMetadata();
            return Results.Ok(metadata);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading temperature metadata: {ex.Message}");
        }
    }

    private static IResult GetIntensityMetadata()
    {
        try
        {
            var metadata = UIMetadataService.GetIntensityMetadata();
            return Results.Ok(metadata);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading intensity metadata: {ex.Message}");
        }
    }

    private static IResult GetDefaults()
    {
        try
        {
            var metadata = UIMetadataService.GetUIMetadata();
            return Results.Ok(new { defaultActivityId = metadata.DefaultActivityId });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading defaults: {ex.Message}");
        }
    }

    private static IResult GetConfigurationMetadata()
    {
        try
        {
            var config = ConfigurationMetadataService.GetConfigurationMetadata();
            return Results.Ok(config);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error loading configuration metadata: {ex.Message}");
        }
    }

    private static IResult CalculateNutritionTargets(TargetsRequest request)
    {
        try
        {
            var athlete = new AthleteProfile(WeightKg: request.AthleteWeightKg);
            var race = new RaceProfile(
                SportType: request.SportType,
                DurationHours: request.DurationHours,
                Temperature: request.Temperature,
                Intensity: request.Intensity
            );
            
            var targets = NutritionCalculator.CalculateTargets(race, athlete);
            
            return Results.Ok(new
            {
                targets.CarbsGPerHour,
                targets.FluidsMlPerHour,
                targets.SodiumMgPerHour,
                TotalCarbsG = targets.CarbsGPerHour * race.DurationHours,
                TotalFluidsML = targets.FluidsMlPerHour * race.DurationHours,
                TotalSodiumMg = targets.SodiumMgPerHour * race.DurationHours
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error calculating nutrition targets: {ex.Message}");
        }
    }
}

public record TargetsRequest(
    double AthleteWeightKg,
    SportType SportType,
    double DurationHours,
    TemperatureCondition Temperature,
    IntensityLevel Intensity
);

public record PlanGenerationRequest(
    double AthleteWeightKg,
    SportType SportType,
    double DurationHours,
    double TemperatureC,
    IntensityLevel Intensity,
    List<ProductRequest>? Products = null,
    ProductFilter? Filter = null,
    int? IntervalMin = null,
    bool? CaffeineEnabled = null
);

public record ProductRequest(
    string Name,
    string ProductType,
    double CarbsG,
    double SodiumMg,
    double VolumeMl,
    double? CaffeineMg = null
);

/// <summary>
/// Response model for advanced nutrition plan generation
/// </summary>
public record AdvancedPlanResponse(
    RaceProfile Race,
    AthleteProfile Athlete,
    List<NutritionEvent> NutritionSchedule,
    ShoppingSummary? ShoppingSummary = null,
    List<string>? Warnings = null,
    List<string>? Errors = null
);
