namespace RaceDay.API.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(b => b.UseSetting("Serilog:MinimumLevel:Default", "Fatal"))
            .CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithNonEmptyArray()
    {
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        (doc.RootElement.GetArrayLength() > 0).ShouldBeTrue("Products array should not be empty");
    }

    [Fact]
    public async Task GetMetadata_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/metadata");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePlan_ValidPayload_ReturnsOkWithNutritionSchedule()
    {
        var payload = new
        {
            athleteWeightKg = 70.0,
            sportType = "Bike",
            durationHours = 2.0,
            temperatureC = 20.0,
            intensity = "Moderate",
            caffeineEnabled = false,
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("nutritionSchedule", out var schedule)
            .ShouldBeTrue($"Response should contain 'nutritionSchedule'. Response: {json}");
        schedule.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task GeneratePlan_MissingBody_ReturnsBadRequestOrUnsupportedMediaType()
    {
        var response = await _client.PostAsync("/api/plan/generate",
            new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

        (response.StatusCode == HttpStatusCode.BadRequest ||
         response.StatusCode == HttpStatusCode.UnsupportedMediaType)
            .ShouldBeTrue($"Expected 400 or 415, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GeneratePlan_NegativeWeight_ReturnsBadRequest()
    {
        var payload = new
        {
            athleteWeightKg = -1.0,
            sportType = "Run",
            durationHours = 1.0,
            temperatureC = 20.0,
            intensity = "Moderate",
            filter = new { }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_ZeroDuration_ReturnsBadRequest()
    {
        var payload = new
        {
            athleteWeightKg = 70.0,
            sportType = "Run",
            durationHours = 0.0,
            temperatureC = 20.0,
            intensity = "Moderate",
            filter = new { }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
