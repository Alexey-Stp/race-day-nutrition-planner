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

    // ── Triathlon leg model (RAC-1) ──────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_ExplicitLegs_ReturnsSegmentTargetsPerLeg()
    {
        var payload = new
        {
            athleteWeightKg = 75.0,
            sportType = "Triathlon",
            durationHours = 10.5,
            temperatureC = 20.0,
            intensity = "Hard",
            caffeineEnabled = false,
            legs = new { swimH = 1.1, bikeH = 5.8, runH = 3.6 },
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("segmentTargets", out var seg)
            .ShouldBeTrue($"Response should contain 'segmentTargets'. Response: {json}");
        seg.ValueKind.ShouldBe(JsonValueKind.Array);
        seg.GetArrayLength().ShouldBe(3);

        // Swim first, ~1.1h = 66 min.
        var swim = seg[0];
        swim.GetProperty("phase").GetString().ShouldBe("Swim");
        swim.GetProperty("durationMinutes").GetDouble().ShouldBe(66.0, tolerance: 0.5);
        swim.GetProperty("carbsG").GetDouble().ShouldBe(0.0);
    }

    [Fact]
    public async Task GeneratePlan_DistancePreset_ReturnsOkWithSegments()
    {
        var payload = new
        {
            athleteWeightKg = 75.0,
            sportType = "Triathlon",
            durationHours = 12.0,
            temperatureC = 20.0,
            intensity = "Hard",
            distancePreset = "Ironman",
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var seg = doc.RootElement.GetProperty("segmentTargets");
        double swimMin = seg[0].GetProperty("durationMinutes").GetDouble();
        // Ironman swim is ~8-10% of 12h (57.6-72 min), definitively under the legacy 20% (144 min).
        (swimMin / 60.0 / 12.0).ShouldBeInRange(0.08, 0.10);
    }

    [Fact]
    public async Task GeneratePlan_LegSumMismatch_ReturnsOkWithNormalisationWarning()
    {
        var payload = new
        {
            athleteWeightKg = 75.0,
            sportType = "Triathlon",
            durationHours = 10.0,
            temperatureC = 20.0,
            intensity = "Hard",
            legs = new { swimH = 1.0, bikeH = 5.0, runH = 3.0 }, // sum 9 != 10
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("normalised");
    }

    [Fact]
    public async Task GeneratePlan_NegativeLeg_ReturnsBadRequest()
    {
        var payload = new
        {
            athleteWeightKg = 75.0,
            sportType = "Triathlon",
            durationHours = 10.0,
            temperatureC = 20.0,
            intensity = "Hard",
            legs = new { swimH = -1.0, bikeH = 5.0, runH = 3.0 },
            filter = new { }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_LegsOnBike_ReturnsOkWithIgnoredWarning()
    {
        var payload = new
        {
            athleteWeightKg = 75.0,
            sportType = "Bike",
            durationHours = 3.0,
            temperatureC = 20.0,
            intensity = "Hard",
            legs = new { swimH = 0.5, bikeH = 1.5, runH = 1.0 },
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("ignored");
    }

    [Fact]
    public async Task GetActivities_IncludesDistancePresetsWithLegFractions()
    {
        var response = await _client.GetAsync("/api/activities");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("triathlon-ironman");
        json.ShouldContain("legFractions");
    }

    // ── Product endpoints ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductById_ValidId_ReturnsOk()
    {
        // First get all products to find a valid ID
        var allResponse = await _client.GetAsync("/api/products");
        allResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await allResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var firstId = doc.RootElement[0].GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/products/{firstId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_InvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/NonExistentProduct_XYZ123");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductsByType_Gel_ReturnsOkWithArray()
    {
        var response = await _client.GetAsync("/api/products/type/gel");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchProducts_WithQuery_ReturnsOkWithArray()
    {
        var response = await _client.GetAsync("/api/products/search?query=gel");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchProducts_EmptyQuery_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/products/search?query=");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Activity endpoints ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActivities_ReturnsOkWithNonEmptyArray()
    {
        var response = await _client.GetAsync("/api/activities");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        (doc.RootElement.GetArrayLength() > 0).ShouldBeTrue();
    }

    [Fact]
    public async Task GetActivityById_ValidId_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/activities/run");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActivityById_InvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/activities/nonexistent_xyz");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActivitiesByType_ValidType_ReturnsOkWithArray()
    {
        var response = await _client.GetAsync("/api/activities/type/Run");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetActivitiesByType_InvalidType_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/activities/type/InvalidSport999");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchActivities_WithQuery_ReturnsOkWithArray()
    {
        var response = await _client.GetAsync("/api/activities/search?query=run");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchActivities_EmptyQuery_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/activities/search?query=");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Metadata endpoints ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTemperatureMetadata_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/metadata/temperatures");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetIntensityMetadata_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/metadata/intensities");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDefaults_ReturnsOkWithDefaultActivityId()
    {
        var response = await _client.GetAsync("/api/metadata/defaults");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("defaultActivityId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task GetConfigurationMetadata_ReturnsOkWithSportConfigs()
    {
        var response = await _client.GetAsync("/api/metadata/configuration");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("sports", out var sports).ShouldBeTrue();
        (sports.GetArrayLength() > 0).ShouldBeTrue();
    }

    // ── Targets endpoint ─────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateNutritionTargets_ValidRequest_ReturnsTargets()
    {
        var payload = new
        {
            athleteWeightKg = 70.0,
            sportType = "Run",
            durationHours = 2.0,
            temperature = "Moderate",
            intensity = "Hard"
        };

        var response = await _client.PostAsJsonAsync("/api/metadata/targets", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("carbsGPerHour", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("fluidsMlPerHour", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task CalculateNutritionTargets_InvalidWeight_ReturnsBadRequest()
    {
        var payload = new
        {
            athleteWeightKg = 0.0,
            sportType = "Run",
            durationHours = 2.0,
            temperature = "Moderate",
            intensity = "Hard"
        };

        var response = await _client.PostAsJsonAsync("/api/metadata/targets", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── GeneratePlan additional cases ────────────────────────────────────────

    [Fact]
    public async Task GeneratePlan_WithFilter_ReturnsOkPlan()
    {
        var payload = new
        {
            athleteWeightKg = 75.0,
            sportType = "Run",
            durationHours = 1.5,
            temperatureC = 20.0,
            intensity = "Moderate",
            caffeineEnabled = false,
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePlan_RunSport_ExcludesDrinksFromFilter()
    {
        // Run sport automatically excludes drink/recovery types when using filter
        var payload = new
        {
            athleteWeightKg = 75.0,
            sportType = "Run",
            durationHours = 1.5,
            temperatureC = 20.0,
            intensity = "Moderate",
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePlan_WithCaffeineEnabled_ReturnsOk()
    {
        var payload = new
        {
            athleteWeightKg = 70.0,
            sportType = "Bike",
            durationHours = 3.0,
            temperatureC = 20.0,
            intensity = "Hard",
            caffeineEnabled = true,
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePlan_ExcessiveWeight_ReturnsBadRequest()
    {
        var payload = new
        {
            athleteWeightKg = 250.0,
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
    public async Task GeneratePlan_ExtremeTemperature_ReturnsBadRequest()
    {
        var payload = new
        {
            athleteWeightKg = 70.0,
            sportType = "Run",
            durationHours = 1.0,
            temperatureC = 100.0, // out of range
            intensity = "Moderate",
            filter = new { }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePlan_WithExplicitProducts_ReturnsOk()
    {
        var payload = new
        {
            athleteWeightKg = 70.0,
            sportType = "Run",
            durationHours = 1.5,
            temperatureC = 20.0,
            intensity = "Moderate",
            products = new[]
            {
                new { name = "Test Gel", productType = "gel", carbsG = 25.0, sodiumMg = 50.0, volumeMl = 0.0 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePlan_HotWeather_ReturnsOkPlan()
    {
        var payload = new
        {
            athleteWeightKg = 70.0,
            sportType = "Bike",
            durationHours = 2.0,
            temperatureC = 30.0,
            intensity = "Moderate",
            filter = new { brand = (string?)null, excludeTypes = (string[]?)null }
        };

        var response = await _client.PostAsJsonAsync("/api/plan/generate", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
