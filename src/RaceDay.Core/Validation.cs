namespace RaceDay.Core;

/// <summary>
/// Provides validation methods for input data
/// </summary>
public static class Validation
{
    /// <summary>
    /// Validates race profile data
    /// </summary>
    public static void ValidateRaceProfile(RaceProfile race)
    {
        if (race.DurationHours <= 0)
            throw new ValidationException(nameof(race.DurationHours), "Duration must be greater than 0");

        if (race.DurationHours > 24)
            throw new ValidationException(nameof(race.DurationHours), "Duration cannot exceed 24 hours");

        if (race.TemperatureC < -20 || race.TemperatureC > 50)
            throw new ValidationException(nameof(race.TemperatureC), "Temperature must be between -20 and 50 degrees Celsius");
    }

    /// <summary>
    /// Validates athlete profile data
    /// </summary>
    public static void ValidateAthleteProfile(AthleteProfile athlete)
    {
        if (athlete.WeightKg <= 0)
            throw new ValidationException(nameof(athlete.WeightKg), "Weight must be greater than 0");

        if (athlete.WeightKg > 250)
            throw new ValidationException(nameof(athlete.WeightKg), "Weight cannot exceed 250 kg");
    }

    /// <summary>
    /// Validates product data
    /// </summary>
    public static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ValidationException(nameof(product.Name), "Product name cannot be empty");

        if (string.IsNullOrWhiteSpace(product.ProductType))
            throw new ValidationException(nameof(product.ProductType), "Product type cannot be empty");

        if (product.CarbsG < 0)
            throw new ValidationException(nameof(product.CarbsG), "Carbohydrates cannot be negative");

        if (product.SodiumMg < 0)
            throw new ValidationException(nameof(product.SodiumMg), "Sodium cannot be negative");

        if (product.VolumeMl < 0)
            throw new ValidationException(nameof(product.VolumeMl), "Volume cannot be negative");

        if (product.ProductType == "drink" && product.VolumeMl <= 0)
            throw new ValidationException(nameof(product.VolumeMl), "Drink products must have a positive volume");
    }

    /// <summary>
    /// Validates interval parameter
    /// </summary>
    public static void ValidateInterval(int intervalMin)
    {
        if (intervalMin <= 0)
            throw new ValidationException(nameof(intervalMin), "Interval must be greater than 0");

        if (intervalMin > 120)
            throw new ValidationException(nameof(intervalMin), "Interval cannot exceed 120 minutes");
    }
}
