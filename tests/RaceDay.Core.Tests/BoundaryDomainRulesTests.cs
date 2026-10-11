namespace RaceDay.Core.Tests;

using RaceDay.Core.Constants;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;
using RaceDay.Core.Services;
using RaceDay.Core.Utilities;

/// <summary>
/// Domain rules moved from the API layer into Core: sport product exclusions, temperature
/// classification, enum/leg guards, target totals and the caffeine default.
/// </summary>
public class BoundaryDomainRulesTests
{
    // ── SportProductRules ────────────────────────────────────────────────────

    [Fact]
    public void ApplySportExclusions_Run_AddsDrinkAndRecovery()
    {
        var filter = SportProductRules.ApplySportExclusions(new ProductFilter(), SportType.Run);

        filter.ExcludeTypes.ShouldNotBeNull();
        filter.ExcludeTypes.ShouldBe(["drink", "recovery"], ignoreOrder: true);
    }

    [Fact]
    public void ApplySportExclusions_RunWithExistingExclusions_DoesNotDuplicateCaseInsensitive()
    {
        var filter = SportProductRules.ApplySportExclusions(new ProductFilter("SiS", ["DRINK", "bar"]), SportType.Run);

        filter.Brand.ShouldBe("SiS");
        filter.ExcludeTypes!.Count.ShouldBe(3);
        filter.ExcludeTypes.ShouldContain("DRINK");
        filter.ExcludeTypes.ShouldContain("bar");
        filter.ExcludeTypes.ShouldContain("recovery");
    }

    [Theory]
    [InlineData(SportType.Bike)]
    [InlineData(SportType.Triathlon)]
    public void ApplySportExclusions_NonRun_ReturnsFilterUnchanged(SportType sport)
    {
        var original = new ProductFilter("Maurten", ["gel"]);

        SportProductRules.ApplySportExclusions(original, sport).ShouldBeSameAs(original);
    }

    [Fact]
    public void ApplySportExclusions_NullFilter_ReturnsFilterWithMandatoryExclusions()
    {
        var filter = SportProductRules.ApplySportExclusions(null, SportType.Run);

        filter.Brand.ShouldBeNull();
        filter.ExcludeTypes!.ShouldContain("drink");
    }

    [Fact]
    public void ExcludedProductTypesFor_Bike_ReturnsEmpty()
    {
        SportProductRules.ExcludedProductTypesFor(SportType.Bike).ShouldBeEmpty();
    }

    // ── TemperatureClassifier ────────────────────────────────────────────────

    [Theory]
    [InlineData(-20, TemperatureCondition.Cold)]
    [InlineData(SchedulingConstraints.ColdTemperatureThresholdC, TemperatureCondition.Cold)]
    [InlineData(5.1, TemperatureCondition.Moderate)]
    [InlineData(20, TemperatureCondition.Moderate)]
    [InlineData(24.9, TemperatureCondition.Moderate)]
    [InlineData(SchedulingConstraints.HotTemperatureThresholdC, TemperatureCondition.Hot)]
    [InlineData(55, TemperatureCondition.Hot)]
    public void FromCelsius_Thresholds_MapsToCondition(double celsius, TemperatureCondition expected)
    {
        TemperatureClassifier.FromCelsius(celsius).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-20.1)]
    [InlineData(55.1)]
    [InlineData(double.NaN)]
    public void FromCelsius_OutOfRange_ThrowsValidationException(double celsius)
    {
        Should.Throw<ValidationException>(() => TemperatureClassifier.FromCelsius(celsius));
    }

    [Fact]
    public void Resolve_NamedConditionOnly_ReturnsCondition()
    {
        TemperatureClassifier.Resolve(TemperatureCondition.Hot, null).ShouldBe(TemperatureCondition.Hot);
    }

    [Fact]
    public void Resolve_CelsiusOnly_ClassifiesCelsius()
    {
        TemperatureClassifier.Resolve(null, 2).ShouldBe(TemperatureCondition.Cold);
    }

    [Fact]
    public void Resolve_BothSupplied_ThrowsValidationException()
    {
        var ex = Should.Throw<ValidationException>(() => TemperatureClassifier.Resolve(TemperatureCondition.Hot, 30));
        ex.PropertyName.ShouldBe("Temperature");
    }

    [Fact]
    public void Resolve_NeitherSupplied_ThrowsValidationException()
    {
        Should.Throw<ValidationException>(() => TemperatureClassifier.Resolve(null, null));
    }

    [Fact]
    public void Resolve_UndefinedNamedCondition_ThrowsValidationException()
    {
        Should.Throw<ValidationException>(() => TemperatureClassifier.Resolve((TemperatureCondition)42, null));
    }

    // ── Validation guards ────────────────────────────────────────────────────

    [Fact]
    public void ValidateDefined_DefinedValue_ReturnsValue()
    {
        Validation.ValidateDefined(SportType.Bike, "SportType").ShouldBe(SportType.Bike);
    }

    [Fact]
    public void ValidateDefined_UndefinedValue_ThrowsWithPropertyName()
    {
        var ex = Should.Throw<ValidationException>(() => Validation.ValidateDefined((IntensityLevel)99, "Intensity"));
        ex.PropertyName.ShouldBe("Intensity");
    }

    [Fact]
    public void ValidateRaceProfile_UndefinedSportType_ThrowsValidationException()
    {
        var race = new RaceProfile((SportType)99, 2, TemperatureCondition.Moderate, IntensityLevel.Moderate);

        Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race)).PropertyName.ShouldBe("SportType");
    }

    [Fact]
    public void ValidateRaceProfile_UndefinedPreset_ThrowsValidationException()
    {
        var race = new RaceProfile(SportType.Triathlon, 2, TemperatureCondition.Moderate, IntensityLevel.Moderate, Preset: (DistancePreset)99);

        Should.Throw<ValidationException>(() => Validation.ValidateRaceProfile(race)).PropertyName.ShouldBe("Preset");
    }

    [Theory]
    [InlineData(-1, 5, 3)]
    [InlineData(0, 0, 0)]
    [InlineData(double.PositiveInfinity, 1, 1)]
    public void ValidateTriathlonLegs_InvalidLegs_ThrowsValidationException(double swim, double bike, double run)
    {
        Should.Throw<ValidationException>(() => Validation.ValidateTriathlonLegs(new TriathlonLegs(swim, bike, run)));
    }

    [Fact]
    public void ValidateTriathlonLegs_SumDiffersFromDuration_DoesNotThrow()
    {
        Should.NotThrow(() => Validation.ValidateTriathlonLegs(new TriathlonLegs(1, 5, 3)));
    }

    // ── Totals and defaults ──────────────────────────────────────────────────

    [Fact]
    public void CalculateMultiNutrientTargets_Totals_EqualPerHourTimesDuration()
    {
        var race = new RaceProfile(SportType.Run, 2.5, TemperatureCondition.Hot, IntensityLevel.Hard);
        var athlete = new AthleteProfile(70);

        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete);

        targets.CarbsG.ShouldBe(targets.CarbsPerHour * 2.5, tolerance: 1e-9);
        targets.FluidMl.ShouldBe(targets.FluidPerHour * 2.5, tolerance: 1e-9);
        targets.SodiumMg.ShouldBe(targets.SodiumPerHour * 2.5, tolerance: 1e-9);
    }

    [Fact]
    public void PlanDefaults_CaffeineEnabled_IsOptIn()
    {
        PlanDefaults.CaffeineEnabled.ShouldBeFalse();
    }
}
