using RaceDay.Core.Services;
using RaceDay.Core.Repositories;
using RaceDay.Core.Models;
using RaceDay.Core.Exceptions;

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
                    p.VolumeMl
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
                    p.VolumeMl
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

            // Generate plan with optional custom interval
            var plan = request.IntervalMin.HasValue
                ? PlanGenerator.Generate(race, athlete, products, request.IntervalMin.Value)
                : PlanGenerator.Generate(race, athlete, products);

            return Results.Ok(plan);
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
}

// Request models for plan generation
public record PlanGenerationRequest(
    double AthleteWeightKg,
    SportType SportType,
    double DurationHours,
    double TemperatureC,
    IntensityLevel Intensity,
    List<ProductRequest>? Products = null,
    ProductFilter? Filter = null,
    int? IntervalMin = null
);

public record ProductRequest(
    string Name,
    string ProductType,
    double CarbsG,
    double SodiumMg,
    double VolumeMl
);
