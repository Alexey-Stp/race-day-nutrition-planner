namespace RaceDay.Core.Tests;

public class ValidationTests
{
    #region ValidateRaceProfile Tests

    [Fact]
    public void ValidateRaceProfile_WithValidData_DoesNotThrow()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: 20, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        Validation.ValidateRaceProfile(race); // Should not throw
    }

    [Fact]
    public void ValidateRaceProfile_ZeroDuration_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 0, TemperatureC: 20, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Duration must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_NegativeDuration_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: -1, TemperatureC: 20, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Duration must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_ExcessiveDuration_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 25, TemperatureC: 20, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Duration cannot exceed 24 hours", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_TooLowTemperature_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: -25, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Temperature must be between -20 and 50 degrees Celsius", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_TooHighTemperature_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: 55, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Temperature must be between -20 and 50 degrees Celsius", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_ExtremeTemperatureBoundaries_ValidatesCorrectly()
    {
        // Arrange
        var raceCold = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: -20, Intensity: IntensityLevel.Moderate);
        var raceHot = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: 50, Intensity: IntensityLevel.Moderate);

        // Act & Assert - should not throw
        Validation.ValidateRaceProfile(raceCold);
        Validation.ValidateRaceProfile(raceHot);
    }

    #endregion

    #region ValidateAthleteProfile Tests

    [Fact]
    public void ValidateAthleteProfile_WithValidData_DoesNotThrow()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);

        // Act & Assert
        Validation.ValidateAthleteProfile(athlete); // Should not throw
    }

    [Fact]
    public void ValidateAthleteProfile_ZeroWeight_ThrowsValidationException()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 0);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateAthleteProfile(athlete));
        Assert.Contains("Weight must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateAthleteProfile_NegativeWeight_ThrowsValidationException()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: -10);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateAthleteProfile(athlete));
        Assert.Contains("Weight must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateAthleteProfile_ExcessiveWeight_ThrowsValidationException()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 300);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateAthleteProfile(athlete));
        Assert.Contains("Weight cannot exceed 250 kg", exception.Message);
    }

    [Fact]
    public void ValidateAthleteProfile_WeightBoundaries_ValidatesCorrectly()
    {
        // Arrange
        var athleteLight = new AthleteProfile(WeightKg: 0.1); // Very light but valid
        var athleteHeavy = new AthleteProfile(WeightKg: 250); // Maximum valid

        // Act & Assert - should not throw
        Validation.ValidateAthleteProfile(athleteLight);
        Validation.ValidateAthleteProfile(athleteHeavy);
    }

    #endregion

    #region ValidateProduct Tests

    [Fact]
    public void ValidateProduct_WithValidData_DoesNotThrow()
    {
        // Arrange
        var product = new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100);

        // Act & Assert
        Validation.ValidateProduct(product); // Should not throw
    }

    [Fact]
    public void ValidateProduct_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("", "gel", CarbsG: 25, SodiumMg: 100);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Product name cannot be empty", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NullName_ThrowsValidationException()
    {
        // Arrange
        var product = new Product(null!, "gel", CarbsG: 25, SodiumMg: 100);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Product name cannot be empty", exception.Message);
    }

    [Fact]
    public void ValidateProduct_EmptyProductType_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test", "", CarbsG: 25, SodiumMg: 100);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Product type cannot be empty", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NegativeCarbs_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test", "gel", CarbsG: -5, SodiumMg: 100);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Carbohydrates cannot be negative", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NegativeSodium_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test", "gel", CarbsG: 25, SodiumMg: -10);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Sodium cannot be negative", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NegativeVolume_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test Drink", "drink", CarbsG: 30, SodiumMg: 200, VolumeMl: -500);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Volume cannot be negative", exception.Message);
    }

    [Fact]
    public void ValidateProduct_DrinkWithZeroVolume_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test Drink", "drink", CarbsG: 30, SodiumMg: 200, VolumeMl: 0);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Drink products must have a positive volume", exception.Message);
    }

    [Fact]
    public void ValidateProduct_GelWithZeroVolume_DoesNotThrow()
    {
        // Arrange - gel products don't require volume
        var product = new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100, VolumeMl: 0);

        // Act & Assert
        Validation.ValidateProduct(product); // Should not throw
    }

    #endregion

    #region ValidateInterval Tests

    [Fact]
    public void ValidateInterval_WithValidInterval_DoesNotThrow()
    {
        // Arrange
        int interval = 30;

        // Act & Assert
        Validation.ValidateInterval(interval); // Should not throw
    }

    [Fact]
    public void ValidateInterval_ZeroInterval_ThrowsValidationException()
    {
        // Arrange
        int interval = 0;

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateInterval(interval));
        Assert.Contains("Interval must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateInterval_NegativeInterval_ThrowsValidationException()
    {
        // Arrange
        int interval = -15;

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateInterval(interval));
        Assert.Contains("Interval must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateInterval_ExcessiveInterval_ThrowsValidationException()
    {
        // Arrange
        int interval = 150;

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => Validation.ValidateInterval(interval));
        Assert.Contains("Interval cannot exceed 120 minutes", exception.Message);
    }

    [Fact]
    public void ValidateInterval_BoundaryIntervals_ValidatesCorrectly()
    {
        // Arrange - minimum and maximum valid values
        int intervalMin = 1;
        int intervalMax = 120;

        // Act & Assert - should not throw
        Validation.ValidateInterval(intervalMin);
        Validation.ValidateInterval(intervalMax);
    }

    #endregion
}
