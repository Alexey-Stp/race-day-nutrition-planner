namespace RaceDay.API.Contracts;

using RaceDay.Core.Models;

// Response DTOs. Property names and order define the public JSON contract (camelCase on the wire);
// they are mapped explicitly from Core records so a Core rename cannot silently change the API.

/// <summary>Response for <c>POST /api/plan/generate</c>.</summary>
public record AdvancedPlanResponse(
    RaceResponse Race,
    AthleteResponse Athlete,
    IReadOnlyList<NutritionEventResponse> NutritionSchedule,
    ShoppingSummaryResponse? ShoppingSummary,
    IReadOnlyList<string>? Warnings,
    IReadOnlyList<string>? Errors,
    IReadOnlyList<SegmentTargetResponse>? SegmentTargets,
    PlanTargetsResponse Targets,
    bool CaffeineEnabled
);

public record RaceResponse(
    SportType SportType,
    double DurationHours,
    TemperatureCondition Temperature,
    IntensityLevel Intensity,
    TriathlonLegsResponse? Legs = null,
    DistancePreset? Preset = null
);

/// <summary>Echo of the request legs. <c>SumH</c> is kept because the original contract serialised it.</summary>
public record TriathlonLegsResponse(double SwimH, double BikeH, double RunH, double SumH);

public record AthleteResponse(double WeightKg);

public record NutritionEventResponse(
    int TimeMin,
    RacePhase Phase,
    string PhaseDescription,
    string ProductName,
    double AmountPortions,
    string Action,
    double TotalCarbsSoFar,
    bool HasCaffeine,
    double? CaffeineMg,
    double TotalCaffeineSoFar,
    double CarbsInEvent,
    double? SipMl,
    EventKind Kind
);

public record ShoppingSummaryResponse(
    IReadOnlyList<ShoppingItemResponse> Items,
    int TotalProductCount,
    double TotalCarbs
);

public record ShoppingItemResponse(string ProductName, double TotalPortions, double TotalCarbs);

/// <summary>Per-leg nutrition targets for a triathlon, ordered swim → bike → run.</summary>
public record SegmentTargetResponse(
    string Phase,
    double CarbsG,
    double SodiumMg,
    double FluidMl,
    double DurationMinutes
);

/// <summary>Whole-race targets the plan was generated against.</summary>
public record PlanTargetsResponse(
    double CarbsGPerHour,
    double FluidsMlPerHour,
    double SodiumMgPerHour,
    double TotalCarbsG,
    double TotalFluidsMl,
    double TotalSodiumMg,
    double CaffeineMg
);

/// <summary>Response for <c>POST /api/metadata/targets</c> (field names preserved from the original contract).</summary>
public record NutritionTargetsResponse(
    double CarbsGPerHour,
    double FluidsMlPerHour,
    double SodiumMgPerHour,
    double TotalCarbsG,
    double TotalFluidsML,
    double TotalSodiumMg
);

/// <summary>Response for <c>GET /api/metadata/defaults</c>.</summary>
public record DefaultsResponse(string DefaultActivityId);

public record ProductResponse(
    string Id,
    string Brand,
    string Name,
    string ProductType,
    double CarbsG,
    double SodiumMg,
    double CaloriesKcal,
    double VolumeMl,
    string ImageUrl,
    double? CaffeineMg = null
);

public record ActivityResponse(
    string Id,
    string Name,
    SportType SportType,
    string Description,
    double MinDurationHours,
    double MaxDurationHours,
    double BestTimeHours,
    DistancePreset? Preset = null,
    LegFractionsResponse? LegFractions = null
);

public record LegFractionsResponse(double Swim, double Bike, double Run);
