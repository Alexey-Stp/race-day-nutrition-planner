namespace RaceDay.Core.Utilities;
using RaceDay.Core.Constants;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;

/// <summary>
/// Provides validation methods for input data. Limits come from <see cref="ValidationLimits"/>.
/// </summary>
public static class Validation
{
    /// <summary>
    /// Validates race profile data
    /// </summary>
    public static void ValidateRaceProfile(RaceProfile race)
    {
        ArgumentNullException.ThrowIfNull(race);
        RequireFinite(race.DurationHours, nameof(race.DurationHours));

        if (race.DurationHours <= 0)
            throw new ValidationException(nameof(race.DurationHours), "Duration must be greater than 0");

        if (race.DurationHours < ValidationLimits.MinDurationHours)
            throw new ValidationException(nameof(race.DurationHours),
                $"Duration must be at least {ValidationLimits.MinDurationHours} hours");

        if (race.DurationHours > ValidationLimits.MaxDurationHours)
            throw new ValidationException(nameof(race.DurationHours),
                $"Duration cannot exceed {ValidationLimits.MaxDurationHours} hours");

        ValidateDefined(race.SportType, nameof(race.SportType));
        ValidateDefined(race.Temperature, nameof(race.Temperature));
        ValidateDefined(race.Intensity, nameof(race.Intensity));
        if (race.Preset is { } preset)
            ValidateDefined(preset, nameof(race.Preset));

        if (race.Legs is { } legs)
        {
            RequireFinite(legs.SwimH, nameof(legs.SwimH));
            RequireFinite(legs.BikeH, nameof(legs.BikeH));
            RequireFinite(legs.RunH, nameof(legs.RunH));
        }
    }

    /// <summary>
    /// Validates athlete profile data
    /// </summary>
    public static void ValidateAthleteProfile(AthleteProfile athlete)
    {
        ArgumentNullException.ThrowIfNull(athlete);
        RequireFinite(athlete.WeightKg, nameof(athlete.WeightKg));

        if (athlete.WeightKg <= 0)
            throw new ValidationException(nameof(athlete.WeightKg), "Weight must be greater than 0");

        if (athlete.WeightKg < ValidationLimits.MinWeightKg)
            throw new ValidationException(nameof(athlete.WeightKg),
                $"Weight must be at least {ValidationLimits.MinWeightKg} kg");

        if (athlete.WeightKg > ValidationLimits.MaxWeightKg)
            throw new ValidationException(nameof(athlete.WeightKg),
                $"Weight cannot exceed {ValidationLimits.MaxWeightKg} kg");
    }

    /// <summary>
    /// Validates an ambient temperature in degrees Celsius (used before mapping to <see cref="TemperatureCondition"/>).
    /// </summary>
    public static void ValidateTemperatureC(double temperatureC)
    {
        RequireFinite(temperatureC, nameof(temperatureC));

        if (temperatureC < ValidationLimits.MinTemperatureC || temperatureC > ValidationLimits.MaxTemperatureC)
            throw new ValidationException(nameof(temperatureC),
                $"Temperature must be between {ValidationLimits.MinTemperatureC} and {ValidationLimits.MaxTemperatureC} °C");
    }

    /// <summary>
    /// Validates product data
    /// </summary>
    public static void ValidateProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ValidationException(nameof(product.Name), "Product name cannot be empty");

        if (string.IsNullOrWhiteSpace(product.ProductType))
            throw new ValidationException(nameof(product.ProductType), "Product type cannot be empty");

        RequireFinite(product.CarbsG, nameof(product.CarbsG));
        RequireFinite(product.SodiumMg, nameof(product.SodiumMg));
        RequireFinite(product.VolumeMl, nameof(product.VolumeMl));
        if (product.CaffeineMg is { } caffeine)
            RequireFinite(caffeine, nameof(product.CaffeineMg));

        if (product.CarbsG < 0)
            throw new ValidationException(nameof(product.CarbsG), "Carbohydrates cannot be negative");

        if (product.SodiumMg < 0)
            throw new ValidationException(nameof(product.SodiumMg), "Sodium cannot be negative");

        if (product.VolumeMl < 0)
            throw new ValidationException(nameof(product.VolumeMl), "Volume cannot be negative");

        if (product.CaffeineMg < 0)
            throw new ValidationException(nameof(product.CaffeineMg), "Caffeine cannot be negative");

        if (product.ProductType.Trim().Equals("drink", StringComparison.OrdinalIgnoreCase) && product.VolumeMl <= 0)
            throw new ValidationException(nameof(product.VolumeMl), "Drink products must have a positive volume");
    }

    /// <summary>
    /// Validates the number of products supplied for one plan.
    /// </summary>
    public static void ValidateProductCount(int count)
    {
        if (count > ValidationLimits.MaxProductsPerPlan)
            throw new ValidationException("Products",
                $"At most {ValidationLimits.MaxProductsPerPlan} products can be used for one plan (got {count})");
    }

    /// <summary>
    /// Validates interval parameter
    /// </summary>
    public static void ValidateInterval(int intervalMin)
    {
        if (intervalMin < ValidationLimits.MinIntervalMin)
            throw new ValidationException(nameof(intervalMin), "Interval must be greater than 0");

        if (intervalMin > ValidationLimits.MaxIntervalMin)
            throw new ValidationException(nameof(intervalMin),
                $"Interval cannot exceed {ValidationLimits.MaxIntervalMin} minutes");
    }

    /// <summary>
    /// Validates explicit triathlon leg durations independent of sport type. A sum that differs from the
    /// race duration is not an error here (the leg model normalises it with a warning).
    /// </summary>
    public static void ValidateTriathlonLegs(TriathlonLegs legs)
    {
        ArgumentNullException.ThrowIfNull(legs);
        RequireFinite(legs.SwimH, nameof(legs.SwimH));
        RequireFinite(legs.BikeH, nameof(legs.BikeH));
        RequireFinite(legs.RunH, nameof(legs.RunH));

        if (legs.SwimH < 0 || legs.BikeH < 0 || legs.RunH < 0)
            throw new ValidationException("Legs", "Leg durations cannot be negative");

        if (legs.SumH <= 0)
            throw new ValidationException("Legs", "Leg durations must sum to more than 0");
    }

    /// <summary>
    /// Rejects enum values that are not defined members (e.g. an out-of-range integer from JSON).
    /// Returns the value so it can be used inline.
    /// </summary>
    public static TEnum ValidateDefined<TEnum>(TEnum value, string propertyName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ValidationException(propertyName,
                $"'{value}' is not a valid value. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}");
        return value;
    }

    private static void RequireFinite(double value, string propertyName)
    {
        if (!double.IsFinite(value))
            throw new ValidationException(propertyName, "Value must be a finite number");
    }
}
