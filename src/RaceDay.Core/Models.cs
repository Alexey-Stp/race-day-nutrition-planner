namespace RaceDay.Core;

public enum SportType { Run, Bike, Triathlon }
public enum IntensityLevel { Easy, Moderate, Hard }

public record AthleteProfile(double WeightKg);

public record RaceProfile(
    SportType SportType,
    double DurationHours,
    double TemperatureC,
    IntensityLevel Intensity
);

public record Product(
    string Name,
    string ProductType,   // "gel", "drink", "bar"
    double CarbsG,
    double SodiumMg,
    double VolumeMl = 0
);

public record NutritionTargets(
    double CarbsGPerHour,
    double FluidsMlPerHour,
    double SodiumMgPerHour
);

public record IntakeItem(
    int TimeMin,
    string ProductName,
    double AmountPortions
);

public record RaceNutritionPlan(
    RaceProfile Race,
    NutritionTargets Targets,
    List<IntakeItem> Schedule,
    double TotalCarbsG,
    double TotalFluidsMl,
    double TotalSodiumMg
);