namespace RaceDay.Core;

/// <summary>
/// Type of endurance sport
/// </summary>
public enum SportType 
{ 
    /// <summary>Running</summary>
    Run, 
    /// <summary>Cycling</summary>
    Bike, 
    /// <summary>Triathlon (swim/bike/run)</summary>
    Triathlon 
}

/// <summary>
/// Exercise intensity level
/// </summary>
public enum IntensityLevel 
{ 
    /// <summary>Easy/recovery pace</summary>
    Easy, 
    /// <summary>Moderate/steady pace</summary>
    Moderate, 
    /// <summary>Hard/race pace</summary>
    Hard 
}

/// <summary>
/// Athlete characteristics
/// </summary>
/// <param name="WeightKg">Body weight in kilograms</param>
public record AthleteProfile(double WeightKg);

/// <summary>
/// Race or training session characteristics
/// </summary>
/// <param name="SportType">Type of sport</param>
/// <param name="DurationHours">Duration in hours</param>
/// <param name="TemperatureC">Temperature in degrees Celsius</param>
/// <param name="Intensity">Exercise intensity level</param>
public record RaceProfile(
    SportType SportType,
    double DurationHours,
    double TemperatureC,
    IntensityLevel Intensity
);

/// <summary>
/// Nutrition product details
/// </summary>
/// <param name="Name">Product name</param>
/// <param name="ProductType">Type: "gel", "drink", or "bar"</param>
/// <param name="CarbsG">Carbohydrate content in grams</param>
/// <param name="SodiumMg">Sodium content in milligrams</param>
/// <param name="VolumeMl">Volume in milliliters (for drinks)</param>
public record Product(
    string Name,
    string ProductType,
    double CarbsG,
    double SodiumMg,
    double VolumeMl = 0
);

/// <summary>
/// Hourly nutrition targets
/// </summary>
/// <param name="CarbsGPerHour">Carbohydrate target in grams per hour</param>
/// <param name="FluidsMlPerHour">Fluid target in milliliters per hour</param>
/// <param name="SodiumMgPerHour">Sodium target in milligrams per hour</param>
public record NutritionTargets(
    double CarbsGPerHour,
    double FluidsMlPerHour,
    double SodiumMgPerHour
);

/// <summary>
/// Single intake item in the schedule
/// </summary>
/// <param name="TimeMin">Time point in minutes from start</param>
/// <param name="ProductName">Name of the product to consume</param>
/// <param name="AmountPortions">Number of portions to consume</param>
public record IntakeItem(
    int TimeMin,
    string ProductName,
    double AmountPortions
);

/// <summary>
/// Complete race nutrition plan
/// </summary>
/// <param name="Race">Race profile used for calculation</param>
/// <param name="Targets">Hourly nutrition targets</param>
/// <param name="Schedule">Time-based intake schedule</param>
/// <param name="TotalCarbsG">Total carbohydrates in grams</param>
/// <param name="TotalFluidsMl">Total fluids in milliliters</param>
/// <param name="TotalSodiumMg">Total sodium in milligrams</param>
public record RaceNutritionPlan(
    RaceProfile Race,
    NutritionTargets Targets,
    List<IntakeItem> Schedule,
    double TotalCarbsG,
    double TotalFluidsMl,
    double TotalSodiumMg
);

/// <summary>
/// Activity information with time limits and best times
/// </summary>
/// <param name="Id">Unique activity identifier</param>
/// <param name="Name">Display name of the activity</param>
/// <param name="SportType">Associated sport type</param>
/// <param name="Description">Activity description</param>
/// <param name="MinDurationHours">Minimum recommended duration in hours</param>
/// <param name="MaxDurationHours">Maximum typical duration in hours</param>
/// <param name="BestTimeHours">Professional/elite time in hours</param>
/// <param name="BestTimeFormatted">Human-readable elite time (e.g., "3:45")</param>
public record ActivityInfo(
    string Id,
    string Name,
    SportType SportType,
    string Description,
    double MinDurationHours,
    double MaxDurationHours,
    double BestTimeHours,
    string BestTimeFormatted
);