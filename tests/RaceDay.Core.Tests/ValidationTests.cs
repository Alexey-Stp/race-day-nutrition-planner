namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Utilities;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Constants;

public class ValidationTests
{
    #region ValidateRaceProfile Tests

    [Fact]
    public void ValidateRaceProfile_WithValidData_DoesNotThrow()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        Validation.ValidateRaceProfile(race); // Should not throw
    }

    [Fact]
    public void ValidateRaceProfile_ZeroDuration_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 0, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Duration must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_NegativeDuration_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: -1, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Duration must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_ExcessiveDuration_ThrowsValidationException()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 25, Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Moderate);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race));
        Assert.Contains("Duration cannot exceed 24 hours", exception.Message);
    }

    [Fact]
    public void ValidateRaceProfile_ColdTemperature_ValidatesCorrectly()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Cold, Intensity: IntensityLevel.Moderate);

        // Act & Assert - should not throw
        Validation.ValidateRaceProfile(race);
    }

    [Fact]
    public void ValidateRaceProfile_HotTemperature_ValidatesCorrectly()
    {
        // Arrange
        var race = new RaceProfile(SportType.Run, DurationHours: 2, Temperature: TemperatureCondition.Hot, Intensity: IntensityLevel.Moderate);

        // Act & Assert - should not throw
        Validation.ValidateRaceProfile(race);
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
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateAthleteProfile(athlete));
        Assert.Contains("Weight must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateAthleteProfile_NegativeWeight_ThrowsValidationException()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: -10);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateAthleteProfile(athlete));
        Assert.Contains("Weight must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateAthleteProfile_ExcessiveWeight_ThrowsValidationException()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 300);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateAthleteProfile(athlete));
        Assert.Contains($"Weight cannot exceed {ValidationLimits.MaxWeightKg} kg", exception.Message);
    }

    [Fact]
    public void ValidateAthleteProfile_WeightBoundaries_ValidatesCorrectly()
    {
        // Arrange
        // Limits are shared with the API via ValidationLimits (1-200 kg).
        var athleteLight = new AthleteProfile(WeightKg: ValidationLimits.MinWeightKg); // Minimum valid
        var athleteHeavy = new AthleteProfile(WeightKg: ValidationLimits.MaxWeightKg); // Maximum valid

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
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Product name cannot be empty", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NullName_ThrowsValidationException()
    {
        // Arrange
        var product = new Product(null!, "gel", CarbsG: 25, SodiumMg: 100);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Product name cannot be empty", exception.Message);
    }

    [Fact]
    public void ValidateProduct_EmptyProductType_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test", "", CarbsG: 25, SodiumMg: 100);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Product type cannot be empty", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NegativeCarbs_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test", "gel", CarbsG: -5, SodiumMg: 100);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Carbohydrates cannot be negative", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NegativeSodium_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test", "gel", CarbsG: 25, SodiumMg: -10);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Sodium cannot be negative", exception.Message);
    }

    [Fact]
    public void ValidateProduct_NegativeVolume_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test Drink", "drink", CarbsG: 30, SodiumMg: 200, VolumeMl: -500);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
        Assert.Contains("Volume cannot be negative", exception.Message);
    }

    [Fact]
    public void ValidateProduct_DrinkWithZeroVolume_ThrowsValidationException()
    {
        // Arrange
        var product = new Product("Test Drink", "drink", CarbsG: 30, SodiumMg: 200, VolumeMl: 0);

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
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
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateInterval(interval));
        Assert.Contains("Interval must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateInterval_NegativeInterval_ThrowsValidationException()
    {
        // Arrange
        int interval = -15;

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateInterval(interval));
        Assert.Contains("Interval must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateInterval_ExcessiveInterval_ThrowsValidationException()
    {
        // Arrange
        int interval = 150;

        // Act & Assert
        var exception = Should.Throw<ValidationException>(() => Validation.ValidateInterval(interval));
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

    #region Finite-number guards and shared limits

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ValidateRaceProfile_NonFiniteDuration_ThrowsValidationException(double duration)
    {
        var race = new RaceProfile(SportType.Run, duration, TemperatureCondition.Moderate, IntensityLevel.Moderate);

        var ex = Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race));
        ex.PropertyName.ShouldBe(nameof(RaceProfile.DurationHours));
    }

    [Fact]
    public void ValidateRaceProfile_NonFiniteLeg_ThrowsValidationException()
    {
        var race = new RaceProfile(SportType.Triathlon, 5, TemperatureCondition.Moderate, IntensityLevel.Hard,
            Legs: new TriathlonLegs(double.NaN, 2.5, 1.5));

        Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ValidateAthleteProfile_NonFiniteWeight_ThrowsValidationException(double weight)
    {
        Should.Throw<ValidationException>(() => Validation.ValidateAthleteProfile(new AthleteProfile(weight)));
    }

    [Theory]
    [InlineData(double.NaN, 0, 0, null)]
    [InlineData(25, double.PositiveInfinity, 0, null)]
    [InlineData(25, 0, double.NaN, null)]
    [InlineData(25, 0, 0, double.NaN)]
    public void ValidateProduct_NonFiniteValue_ThrowsValidationException(double carbs, double sodium, double volume, double? caffeine)
    {
        var product = new Product("Gel", "gel", carbs, sodium, volume, caffeine);

        Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
    }

    [Fact]
    public void ValidateProduct_NegativeCaffeine_ThrowsValidationException()
    {
        Should.Throw<ValidationException>(() => Validation.ValidateProduct(new Product("Gel", "gel", 25, 50, 0, -5)));
    }

    [Theory]
    [InlineData("Drink")]
    [InlineData("DRINK")]
    [InlineData(" drink ")]
    public void ValidateProduct_DrinkTypeAnyCase_RequiresPositiveVolume(string productType)
    {
        var product = new Product("Bottle", productType, 40, 300, VolumeMl: 0);

        Should.Throw<ValidationException>(() => Validation.ValidateProduct(product));
    }

    [Fact]
    public void ValidateRaceProfile_BelowMinimumDuration_ThrowsValidationException()
    {
        var race = new RaceProfile(SportType.Run, ValidationLimits.MinDurationHours / 2, TemperatureCondition.Moderate, IntensityLevel.Moderate);

        Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race));
    }

    [Fact]
    public void ValidateRaceProfile_DurationBoundaries_DoNotThrow()
    {
        Validation.ValidateRaceProfile(new RaceProfile(SportType.Run, ValidationLimits.MinDurationHours, TemperatureCondition.Moderate, IntensityLevel.Moderate));
        Validation.ValidateRaceProfile(new RaceProfile(SportType.Run, ValidationLimits.MaxDurationHours, TemperatureCondition.Moderate, IntensityLevel.Moderate));
    }

    [Fact]
    public void ValidateAthleteProfile_BelowMinimumWeight_ThrowsValidationException()
    {
        Should.Throw<ValidationException>(() => Validation.ValidateAthleteProfile(new AthleteProfile(ValidationLimits.MinWeightKg / 2)));
    }

    [Theory]
    [InlineData(-20)]
    [InlineData(0)]
    [InlineData(55)]
    public void ValidateTemperatureC_WithinRange_DoesNotThrow(double temperature)
    {
        Validation.ValidateTemperatureC(temperature);
    }

    [Theory]
    [InlineData(-20.5)]
    [InlineData(55.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ValidateTemperatureC_OutOfRangeOrNonFinite_ThrowsValidationException(double temperature)
    {
        Should.Throw<ValidationException>(() => Validation.ValidateTemperatureC(temperature));
    }

    [Fact]
    public void ValidateProductCount_AboveLimit_ThrowsValidationException()
    {
        Validation.ValidateProductCount(ValidationLimits.MaxProductsPerPlan);
        Should.Throw<ValidationException>(() => Validation.ValidateProductCount(ValidationLimits.MaxProductsPerPlan + 1));
    }

    #endregion
}
