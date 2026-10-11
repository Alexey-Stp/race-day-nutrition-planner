using Microsoft.AspNetCore.Http.HttpResults;
using RaceDay.API.Contracts;
using RaceDay.API.Infrastructure;
using RaceDay.API.Metadata;
using RaceDay.Core.Constants;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;
using RaceDay.Core.Services;
using RaceDay.Core.Utilities;

namespace RaceDay.API;

/// <summary>
/// Endpoint definitions. Handlers only map contracts to Core calls; validation and domain rules live in Core,
/// and exceptions are turned into problem responses by <see cref="GlobalExceptionHandler"/>.
/// </summary>
public static class ApiEndpointExtensions
{
    /// <summary>
    /// Map product-related API endpoints
    /// </summary>
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/", GetAllProductsAsync)
            .WithName("GetAllProducts")
            .WithDescription("Retrieve all available products");

        group.MapGet("/{id}", GetProductByIdAsync)
            .WithName("GetProductById")
            .WithDescription("Get a specific product by ID");

        group.MapGet("/type/{type}", GetProductsByTypeAsync)
            .WithName("GetProductsByType")
            .WithDescription("Filter products by type (gel, drink, bar)");

        group.MapGet("/search", SearchProductsAsync)
            .WithName("SearchProducts")
            .WithDescription("Search products by name or brand");
    }

    /// <summary>
    /// Map activity-related API endpoints
    /// </summary>
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activities")
            .WithTags("Activities")
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/", GetAllActivitiesAsync)
            .WithName("GetAllActivities")
            .WithDescription("Retrieve all predefined activities");

        group.MapGet("/{id}", GetActivityByIdAsync)
            .WithName("GetActivityById")
            .WithDescription("Get a specific activity by ID");

        group.MapGet("/type/{sportType}", GetActivitiesByTypeAsync)
            .WithName("GetActivitiesByType")
            .WithDescription("Filter activities by sport type (Run, Bike, Triathlon)");

        group.MapGet("/search", SearchActivitiesAsync)
            .WithName("SearchActivities")
            .WithDescription("Search activities by name or description");
    }

    /// <summary>
    /// Map nutrition plan generation API endpoints (rate limited per client IP)
    /// </summary>
    public static void MapPlanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/plan")
            .WithTags("Nutrition Plan")
            .RequireRateLimiting(ApiLimits.PlanRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/generate", GeneratePlanAsync)
            .WithName("GeneratePlan")
            .WithDescription("Generate a nutrition plan based on race parameters, athlete profile, and available products. " +
                           "Supply exactly one of 'temperature' (Cold/Moderate/Hot) or 'temperatureC'. " +
                           "When using a filter with Run sport, drinks and recovery products are automatically excluded.");
    }

    /// <summary>
    /// Map UI metadata API endpoints (descriptions, ranges, etc. for frontend)
    /// </summary>
    public static void MapMetadataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/metadata")
            .WithTags("Metadata")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

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
            .WithDescription("Calculate nutrition targets for a specific athlete and race parameters")
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);
    }

    // ── Product handlers ─────────────────────────────────────────────────────

    private static async Task<Ok<List<ProductResponse>>> GetAllProductsAsync(
        IProductRepository repository, CancellationToken cancellationToken) =>
        TypedResults.Ok(ToResponses(await repository.GetAllProductsAsync(cancellationToken)));

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetProductByIdAsync(
        string id, IProductRepository repository, CancellationToken cancellationToken) =>
        await repository.GetProductByIdAsync(id, cancellationToken) is { } product
            ? TypedResults.Ok(product.ToResponse())
            : TypedResults.NotFound();

    private static async Task<Ok<List<ProductResponse>>> GetProductsByTypeAsync(
        string type, IProductRepository repository, CancellationToken cancellationToken) =>
        TypedResults.Ok(ToResponses(await repository.GetProductsByTypeAsync(type, cancellationToken)));

    private static async Task<Results<Ok<List<ProductResponse>>, ValidationProblem>> SearchProductsAsync(
        string? query, IProductRepository repository, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ApiProblems.ValidationResult("query", "Search query is required");

        return TypedResults.Ok(ToResponses(await repository.SearchProductsAsync(query, cancellationToken)));
    }

    // ── Activity handlers ────────────────────────────────────────────────────

    private static async Task<Ok<List<ActivityResponse>>> GetAllActivitiesAsync(
        IActivityRepository repository, CancellationToken cancellationToken) =>
        TypedResults.Ok(ToResponses(await repository.GetAllActivitiesAsync(cancellationToken)));

    private static async Task<Results<Ok<ActivityResponse>, NotFound>> GetActivityByIdAsync(
        string id, IActivityRepository repository, CancellationToken cancellationToken) =>
        await repository.GetActivityByIdAsync(id, cancellationToken) is { } activity
            ? TypedResults.Ok(activity.ToResponse())
            : TypedResults.NotFound();

    private static async Task<Results<Ok<List<ActivityResponse>>, ValidationProblem>> GetActivitiesByTypeAsync(
        string sportType, IActivityRepository repository, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SportType>(sportType, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return ApiProblems.ValidationResult(nameof(sportType),
                $"Invalid sport type. Valid values: {string.Join(", ", Enum.GetNames<SportType>())}");

        return TypedResults.Ok(ToResponses(await repository.GetActivitiesBySportTypeAsync(parsed, cancellationToken)));
    }

    private static async Task<Results<Ok<List<ActivityResponse>>, ValidationProblem>> SearchActivitiesAsync(
        string? query, IActivityRepository repository, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ApiProblems.ValidationResult("query", "Search query is required");

        return TypedResults.Ok(ToResponses(await repository.SearchActivitiesAsync(query, cancellationToken)));
    }

    // ── Plan handler ─────────────────────────────────────────────────────────

    private static async Task<Results<Ok<AdvancedPlanResponse>, ValidationProblem>> GeneratePlanAsync(
        PlanGenerationRequest request,
        IProductRepository repository,
        INutritionPlanService planService,
        CancellationToken cancellationToken)
    {
        var temperature = TemperatureClassifier.Resolve(request.Temperature, request.TemperatureC);

        TriathlonLegs? legs = null;
        if (request.Legs is { } legsRequest)
        {
            legs = legsRequest.ToDomain();
            Validation.ValidateTriathlonLegs(legs);
        }

        var (products, problem) = await LoadProductsAsync(request, repository, cancellationToken);
        if (problem is not null)
            return problem;

        var athlete = new AthleteProfile(request.AthleteWeightKg);
        var race = new RaceProfile(request.SportType, request.DurationHours, temperature, request.Intensity, legs, request.DistancePreset);
        var caffeineEnabled = request.CaffeineEnabled ?? PlanDefaults.CaffeineEnabled;

        var plan = planService.GeneratePlanWithDiagnostics(race, athlete, products, caffeineEnabled);
        var targets = plan.Targets ?? NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled);

        return TypedResults.Ok(plan.ToResponse(race, athlete, targets, caffeineEnabled));
    }

    /// <summary>
    /// Products come from the explicit list when non-empty, otherwise from the catalogue filter
    /// (with the sport's mandatory exclusions applied).
    /// </summary>
    private static async Task<(IReadOnlyList<Product> Products, ValidationProblem? Problem)> LoadProductsAsync(
        PlanGenerationRequest request, IProductRepository repository, CancellationToken cancellationToken)
    {
        if (request.Products is { Count: > 0 } explicitProducts)
        {
            Validation.ValidateProductCount(explicitProducts.Count);
            if (explicitProducts.Any(p => p is null))
                return ([], ApiProblems.ValidationResult("Products", "Product entries cannot be null"));

            return (explicitProducts.Select(p => p.ToDomain()).ToList(), null);
        }

        if (request.Filter is null)
            return ([], ApiProblems.ValidationResult("Products",
                "Either a non-empty 'products' list or a 'filter' must be provided"));

        var filter = SportProductRules.ApplySportExclusions(request.Filter.ToDomain(), request.SportType);
        var catalogueProducts = await repository.GetFilteredProductsAsync(filter, cancellationToken);
        if (catalogueProducts.Count == 0)
            return ([], ApiProblems.ValidationResult("Filter", "No products found matching the specified filter"));

        return (catalogueProducts.Select(p => p.ToPlannerProduct()).ToList(), null);
    }

    // ── Metadata handlers ────────────────────────────────────────────────────

    private static Ok<UIMetadata> GetUIMetadata(IUIMetadataService metadata) =>
        TypedResults.Ok(metadata.GetUIMetadata());

    private static Ok<IReadOnlyList<TemperatureMetadata>> GetTemperatureMetadata(IUIMetadataService metadata) =>
        TypedResults.Ok(metadata.GetTemperatureMetadata());

    private static Ok<IReadOnlyList<IntensityMetadata>> GetIntensityMetadata(IUIMetadataService metadata) =>
        TypedResults.Ok(metadata.GetIntensityMetadata());

    private static Ok<DefaultsResponse> GetDefaults(IUIMetadataService metadata) =>
        TypedResults.Ok(new DefaultsResponse(metadata.GetUIMetadata().DefaultActivityId));

    private static Ok<ConfigurationMetadata> GetConfigurationMetadata(IConfigurationMetadataService configuration) =>
        TypedResults.Ok(configuration.GetConfigurationMetadata());

    private static Results<Ok<NutritionTargetsResponse>, ValidationProblem> CalculateNutritionTargets(TargetsRequest request)
    {
        var athlete = new AthleteProfile(request.AthleteWeightKg);
        var race = new RaceProfile(request.SportType, request.DurationHours, request.Temperature, request.Intensity);
        Validation.ValidateAthleteProfile(athlete);
        Validation.ValidateRaceProfile(race);

        return TypedResults.Ok(NutritionCalculator.CalculateMultiNutrientTargets(race, athlete).ToTargetsResponse());
    }

    private static List<ProductResponse> ToResponses(IEnumerable<ProductInfo> products) =>
        products.Select(p => p.ToResponse()).ToList();

    private static List<ActivityResponse> ToResponses(IEnumerable<ActivityInfo> activities) =>
        activities.Select(a => a.ToResponse()).ToList();
}
