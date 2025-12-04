namespace RaceDay.Core.Tests;

public class PlanGeneratorTests
{
    [Fact]
    public void Generate_WithValidInputs_CreatesNutritionPlan()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(race, plan.Race);
        Assert.NotNull(plan.Targets);
        Assert.NotNull(plan.Schedule);
    }

    [Fact]
    public void Generate_WithoutGelProduct_ThrowsException()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act & Assert
        var exception = Assert.Throws<MissingProductException>(() => PlanGenerator.Generate(race, athlete, products));
        Assert.Equal("gel", exception.ProductType);
    }

    [Fact]
    public void Generate_WithoutDrinkProduct_ThrowsException()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100)
        };

        // Act & Assert
        var exception = Assert.Throws<MissingProductException>(() => PlanGenerator.Generate(race, athlete, products));
        Assert.Equal("drink", exception.ProductType);
    }

    [Fact]
    public void Generate_ScheduleHasCorrectIntervals()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products, intervalMin: 20);

        // Assert
        var times = plan.Schedule.Select(s => s.TimeMin).Distinct().ToList();
        Assert.Contains(0, times);
        Assert.Contains(20, times);
        Assert.Contains(40, times);
    }

    [Fact]
    public void Generate_CalculatesTotalNutrition()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products);

        // Assert
        Assert.True(plan.TotalCarbsG > 0);
        Assert.True(plan.TotalFluidsMl > 0);
        Assert.True(plan.TotalSodiumMg > 0);
    }

    [Fact]
    public void Generate_CustomInterval_UsesCorrectInterval()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products, intervalMin: 15);

        // Assert
        var times = plan.Schedule.Select(s => s.TimeMin).Distinct().ToList();
        Assert.Contains(0, times);
        Assert.Contains(15, times);
        Assert.Contains(30, times);
        Assert.Contains(45, times);
    }

    [Fact]
    public void Generate_IncludesProductSummaries()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products);

        // Assert
        Assert.NotNull(plan.ProductSummaries);
        Assert.NotEmpty(plan.ProductSummaries);
    }

    [Fact]
    public void Generate_ProductSummaries_AggregatesCorrectly()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Test Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Test Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products);

        // Assert
        var gelSummary = plan.ProductSummaries.FirstOrDefault(s => s.ProductName == "Test Gel");
        var drinkSummary = plan.ProductSummaries.FirstOrDefault(s => s.ProductName == "Test Drink 500ml");
        
        Assert.NotNull(gelSummary);
        Assert.NotNull(drinkSummary);
        
        // Verify that the sum of portions in schedule matches the summary
        var gelPortionsInSchedule = plan.Schedule
            .Where(item => item.ProductName == "Test Gel")
            .Sum(item => item.AmountPortions);
        var drinkPortionsInSchedule = plan.Schedule
            .Where(item => item.ProductName == "Test Drink 500ml")
            .Sum(item => item.AmountPortions);
        
        Assert.Equal(gelPortionsInSchedule, gelSummary.TotalPortions);
        Assert.Equal(drinkPortionsInSchedule, drinkSummary.TotalPortions);
    }

    [Fact]
    public void Generate_ProductSummaries_OrderedByProductName()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 1, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Zinc Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Alpha Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products);

        // Assert
        Assert.Equal(2, plan.ProductSummaries.Count);
        Assert.Equal("Alpha Drink 500ml", plan.ProductSummaries[0].ProductName);
        Assert.Equal("Zinc Gel", plan.ProductSummaries[1].ProductName);
    }

    [Fact]
    public void Generate_ProductSummaries_ReflectsAllScheduleItems()
    {
        // Arrange
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 2, TemperatureC: 20, Intensity: IntensityLevel.Moderate);
        var products = new List<Product>
        {
            new Product("Energy Gel", "gel", CarbsG: 25, SodiumMg: 100),
            new Product("Sports Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
        };

        // Act
        var plan = PlanGenerator.Generate(race, athlete, products);

        // Assert - Total portions in summaries should match total schedule items
        var totalSummaryPortions = plan.ProductSummaries.Sum(s => s.TotalPortions);
        var totalSchedulePortions = plan.Schedule.Sum(item => item.AmountPortions);
        
        Assert.Equal(totalSchedulePortions, totalSummaryPortions);
    }
}
