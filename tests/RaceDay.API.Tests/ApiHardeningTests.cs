namespace RaceDay.API.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;
using RaceDay.Core.Services;

/// <summary>
/// Error contract (problem+json), validation at the boundary, limits, CORS and health.
/// </summary>
public class ApiHardeningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ProblemJson = "application/problem+json";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiHardeningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b => b.UseSetting("Serilog:MinimumLevel:Default", "Fatal"));
        _client = _factory.CreateClient();
    }

    private static object ValidPlan(object? products = null, object? filter = null) => new
    {
        athleteWeightKg = 70.0,
        sportType = "Bike",
        durationHours = 2.0,
        temperature = "Moderate",
        intensity = "Moderate",
        products,
        filter
    };

    private static object Gel(string? name = "Gel", string? type = "gel", double carbs = 25) =>
        new { name, productType = type, carbsG = carbs, sodiumMg = 50.0, volumeMl = 0.0 };

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(expected, body);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson, body);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement.Clone();
        root.GetProperty("status").GetInt32().ShouldBe((int)expected);
        root.TryGetProperty("title", out _).ShouldBeTrue();
        return root;
    }

    private static async Task<JsonElement> ReadValidationProblemAsync(HttpResponseMessage response, string errorKey)
    {
        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        problem.TryGetProperty("errors", out var errors).ShouldBeTrue(problem.ToString());
        errors.TryGetProperty(errorKey, out var messages).ShouldBeTrue($"Expected error key '{errorKey}' in {errors}");
        messages.GetArrayLength().ShouldBeGreaterThan(0);
        return problem;
    }

    private WebApplicationFactory<Program> WithServices(Action<IServiceCollection> configure) =>
        _factory.WithWebHostBuilder(b => b.ConfigureTestServices(configure));

    // ── Explicit product validation ──────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_NegativeCarbsProduct_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel(carbs: -5) }));

        await ReadValidationProblemAsync(response, "CarbsG");
    }

    [Fact]
    public async Task GeneratePlan_NullProductName_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel(name: null) }));

        await ReadValidationProblemAsync(response, "Name");
    }

    [Fact]
    public async Task GeneratePlan_UnknownProductType_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel(type: "pizza") }));

        await ReadValidationProblemAsync(response, "ProductType");
    }

    [Fact]
    public async Task GeneratePlan_MoreThanFiftyProducts_ReturnsValidationProblem()
    {
        var products = Enumerable.Range(0, 51).Select(i => Gel(name: $"Gel {i}")).ToArray();

        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: products));

        await ReadValidationProblemAsync(response, "Products");
    }

    [Fact]
    public async Task GeneratePlan_EmptyProductsWithoutFilter_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: Array.Empty<object>()));

        await ReadValidationProblemAsync(response, "Products");
    }

    [Fact]
    public async Task GeneratePlan_NullProductEntry_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new object?[] { null }));

        await ReadValidationProblemAsync(response, "Products");
    }

    [Fact]
    public async Task GeneratePlan_FilterMatchesNothing_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(filter: new { brand = "NoSuchBrand" }));

        await ReadValidationProblemAsync(response, "Filter");
    }

    // ── Malformed input and enums ────────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_MalformedJson_ReturnsBadRequestProblem()
    {
        var response = await _client.PostAsync("/api/plan/generate",
            new StringContent("{\"athleteWeightKg\": 70,", Encoding.UTF8, "application/json"));

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_UnknownEnumName_ReturnsBadRequestProblem()
    {
        var payload = new { athleteWeightKg = 70.0, sportType = "Swim", durationHours = 2.0, temperature = "Moderate", intensity = "Hard", filter = new { } };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_UndefinedIntegerEnum_ReturnsBadRequestProblem()
    {
        var payload = new { athleteWeightKg = 70.0, sportType = 99, durationHours = 2.0, temperature = "Moderate", intensity = "Hard", filter = new { } };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_TemperatureAsInteger_ReturnsBadRequestProblem()
    {
        var payload = new { athleteWeightKg = 70.0, sportType = "Run", durationHours = 2.0, temperature = 7, intensity = "Hard", filter = new { } };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_DefinedIntegerEnums_ReturnsBadRequestProblem()
    {
        // Enums must be sent by name; even defined numeric values are rejected.
        var payload = new
        {
            athleteWeightKg = 70.0, sportType = 1, durationHours = 2.0, temperature = "Hot", intensity = 2,
            caffeineEnabled = false, products = new[] { Gel() }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_EnumNames_ReturnsOk()
    {
        // Shape sent by the React client: all enums by name.
        var payload = new
        {
            athleteWeightKg = 70.0, sportType = "Bike", durationHours = 2.0, temperature = "Hot", intensity = "Hard",
            caffeineEnabled = false, products = new[] { Gel() }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    // ── Temperature contract ─────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_TemperatureEnumOnly_ReturnsOkWithCondition()
    {
        var response = await _client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel() }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("race").GetProperty("temperature").GetString().ShouldBe("Moderate");
    }

    [Fact]
    public async Task GeneratePlan_TemperatureCelsiusOnly_ReturnsOkWithMappedCondition()
    {
        var payload = new { athleteWeightKg = 70.0, sportType = "Bike", durationHours = 2.0, temperatureC = 30.0, intensity = "Moderate", products = new[] { Gel() } };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("race").GetProperty("temperature").GetString().ShouldBe("Hot");
    }

    [Fact]
    public async Task GeneratePlan_BothTemperatureFields_ReturnsValidationProblem()
    {
        var payload = new { athleteWeightKg = 70.0, sportType = "Bike", durationHours = 2.0, temperature = "Hot", temperatureC = 30.0, intensity = "Moderate", products = new[] { Gel() } };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        await ReadValidationProblemAsync(response, "Temperature");
    }

    [Fact]
    public async Task GeneratePlan_NoTemperatureField_ReturnsValidationProblem()
    {
        var payload = new { athleteWeightKg = 70.0, sportType = "Bike", durationHours = 2.0, intensity = "Moderate", products = new[] { Gel() } };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        await ReadValidationProblemAsync(response, "Temperature");
    }

    // ── Response contract ────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_ValidRequest_ResponseIncludesTargetsCaffeineFlagAndEventKinds()
    {
        var payload = new
        {
            athleteWeightKg = 70.0, sportType = "Bike", durationHours = 3.0, temperature = "Moderate", intensity = "Hard",
            caffeineEnabled = true, filter = new { }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("caffeineEnabled").GetBoolean().ShouldBeTrue();

        var targets = root.GetProperty("targets");
        foreach (var name in new[] { "carbsGPerHour", "fluidsMlPerHour", "sodiumMgPerHour", "totalCarbsG", "totalFluidsMl", "totalSodiumMg", "caffeineMg" })
            targets.TryGetProperty(name, out _).ShouldBeTrue($"targets.{name} missing");
        targets.GetProperty("totalCarbsG").GetDouble()
            .ShouldBe(targets.GetProperty("carbsGPerHour").GetDouble() * 3.0, tolerance: 0.001);
        targets.GetProperty("caffeineMg").GetDouble().ShouldBeGreaterThan(0);

        var kinds = root.GetProperty("nutritionSchedule").EnumerateArray()
            .Select(e => e.GetProperty("kind").GetString())
            .ToList();
        kinds.ShouldNotBeEmpty();
        kinds.ShouldAllBe(k => k == "PreRace" || k == "Sip" || k == "Intake");
    }

    // ── Exception mapping ────────────────────────────────────────────────────

    private sealed class ThrowingPlanService(Exception exception) : INutritionPlanService
    {
        public List<NutritionEvent> GeneratePlan(RaceProfile race, AthleteProfile athlete, IReadOnlyList<Product> products, bool caffeineEnabled = false) => throw exception;
        public PlanResult GeneratePlanWithDiagnostics(RaceProfile race, AthleteProfile athlete, IReadOnlyList<Product> products, bool caffeineEnabled = false) => throw exception;
    }

    private sealed class FailingProductRepository : IProductRepository
    {
        private static CatalogueLoadException Fail() => new("product", "resource missing");
        public Task<IReadOnlyList<ProductInfo>> GetAllProductsAsync(CancellationToken cancellationToken = default) => throw Fail();
        public Task<IReadOnlyList<ProductInfo>> GetProductsByTypeAsync(string productType, CancellationToken cancellationToken = default) => throw Fail();
        public Task<ProductInfo?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default) => throw Fail();
        public Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string query, CancellationToken cancellationToken = default) => throw Fail();
        public Task<IReadOnlyList<ProductInfo>> GetFilteredProductsAsync(ProductFilter? filter, CancellationToken cancellationToken = default) => throw Fail();
    }

    [Fact]
    public async Task GeneratePlan_MissingProductException_ReturnsBadRequestProblem()
    {
        var client = WithServices(s => s.AddSingleton<INutritionPlanService>(new ThrowingPlanService(new MissingProductException("gel")))).CreateClient();

        var response = await client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel() }));

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("detail").GetString()!.ShouldContain("gel");
    }

    [Fact]
    public async Task GeneratePlan_UnexpectedException_ReturnsGenericProblemWithoutInternals()
    {
        var client = WithServices(s => s.AddSingleton<INutritionPlanService>(new ThrowingPlanService(new InvalidOperationException("secret internal detail")))).CreateClient();

        var response = await client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel() }));

        await ReadProblemAsync(response, HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("secret internal detail");
        body.ShouldNotContain("InvalidOperationException");
    }

    [Fact]
    public async Task GetProducts_CatalogueLoadFailure_ReturnsServiceUnavailableProblem()
    {
        var client = WithServices(s => s.AddSingleton<IProductRepository, FailingProductRepository>()).CreateClient();

        var response = await client.GetAsync("/api/products");

        await ReadProblemAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Health_CatalogueLoadFailure_ReturnsServiceUnavailable()
    {
        var client = WithServices(s => s.AddSingleton<IProductRepository, FailingProductRepository>()).CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    // ── Other boundary checks ────────────────────────────────────────────────

    [Fact]
    public async Task CalculateNutritionTargets_DurationOutOfRange_ReturnsValidationProblem()
    {
        var payload = new { athleteWeightKg = 70.0, sportType = "Run", durationHours = 30.0, temperature = "Moderate", intensity = "Hard" };

        var response = await _client.PostAsJsonAsync("/api/metadata/targets", payload);

        await ReadValidationProblemAsync(response, "DurationHours");
    }

    [Fact]
    public async Task CalculateNutritionTargets_WeightOutOfRange_ReturnsValidationProblem()
    {
        var payload = new { athleteWeightKg = 500.0, sportType = "Run", durationHours = 2.0, temperature = "Moderate", intensity = "Hard" };

        var response = await _client.PostAsJsonAsync("/api/metadata/targets", payload);

        await ReadValidationProblemAsync(response, "WeightKg");
    }

    [Fact]
    public async Task SearchProducts_EmptyQuery_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/products/search?query=");

        await ReadValidationProblemAsync(response, "query");
    }

    [Fact]
    public async Task GetActivitiesByType_UndefinedNumericType_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/activities/type/99");

        await ReadValidationProblemAsync(response, "sportType");
    }

    [Fact]
    public async Task GetProductById_UnknownId_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync("/api/products/does-not-exist");

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    // ── Limits ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_OversizedBody_ReturnsPayloadTooLargeProblem()
    {
        var padding = new string('x', 70 * 1024);
        var json = $"{{\"athleteWeightKg\":70,\"sportType\":\"Bike\",\"durationHours\":2,\"temperature\":\"Moderate\",\"intensity\":\"Hard\",\"pad\":\"{padding}\",\"filter\":{{}}}}";

        var response = await _client.PostAsync("/api/plan/generate", new StringContent(json, Encoding.UTF8, "application/json"));

        await ReadProblemAsync(response, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task GeneratePlan_ExceedsRateLimit_ReturnsTooManyRequestsProblem()
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "2",
                ["RateLimiting:WindowSeconds"] = "3600"
            }))).CreateClient();

        for (int i = 0; i < 2; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel() }));
            ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var limited = await client.PostAsJsonAsync("/api/plan/generate", ValidPlan(products: new[] { Gel() }));

        await ReadProblemAsync(limited, HttpStatusCode.TooManyRequests);

        // Health and non-plan endpoints are not rate limited.
        (await client.GetAsync("/health")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/metadata")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── OpenAPI ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Swagger_Development_DocumentsResponseAndProblemSchemas()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("AdvancedPlanResponse");
        json.ShouldContain("PlanTargetsResponse");
        json.ShouldContain("NutritionTargetsResponse");
        json.ShouldContain("HttpValidationProblemDetails");
        json.ShouldContain("\"429\"");
    }

    // ── Health and CORS ──────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Default_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Cors_Development_AllowsLoopbackOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/metadata");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe("http://localhost:5173");
    }

    [Fact]
    public async Task Cors_Development_DeniesNonLoopbackOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/metadata");
        request.Headers.Add("Origin", "https://anything.example");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Cors_ValidationError_StillCarriesCorsHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/plan/generate")
        {
            Content = JsonContent.Create(ValidPlan(products: new[] { Gel(carbs: -1) }))
        };
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe("http://localhost:5173");
    }

    [Fact]
    public async Task Cors_ProductionWithConfiguredOrigins_AllowsOnlyThoseOrigins()
    {
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example"
            }));
        }).CreateClient();

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/metadata");
        allowed.Headers.Add("Origin", "https://app.example");
        var allowedResponse = await client.SendAsync(allowed);
        allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe("https://app.example");

        using var denied = new HttpRequestMessage(HttpMethod.Get, "/api/metadata");
        denied.Headers.Add("Origin", "https://evil.example");
        var deniedResponse = await client.SendAsync(denied);
        deniedResponse.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Cors_ProductionWithoutConfiguredOrigins_DeniesCrossOrigin()
    {
        var client = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production")).CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/metadata");
        request.Headers.Add("Origin", "https://anything.example");
        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task GeneratePlan_MalformedJsonInProduction_ReturnsBadRequestProblem()
    {
        var client = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production")).CreateClient();

        var response = await client.PostAsync("/api/plan/generate",
            new StringContent("{not json", Encoding.UTF8, "application/json"));

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }
}
