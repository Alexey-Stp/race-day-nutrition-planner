namespace RaceDay.Core.Tests;
using RaceDay.Core.Services;
using RaceDay.Core.Models;
using RaceDay.Core.Constants;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Regression test for the mixed-product 4h easy run that used to be a print-only diagnostic.
/// The schedule is still written to the test output for troubleshooting, but every hard rule is asserted.
/// </summary>
public class DiagnosticTest
{
    private readonly ITestOutputHelper _output;
    public DiagnosticTest(ITestOutputHelper output) => _output = output;

    private static List<ProductEnhanced> MixedProducts() => new()
    {
        new("Gel Light", CarbsG: 20, Texture: ProductTexture.LightGel, HasCaffeine: false, CaffeineMg: 0),
        new("SiS Beta Fuel Gel", CarbsG: 40, Texture: ProductTexture.Gel, HasCaffeine: true, CaffeineMg: 75),
        new("SiS Beta Fuel Nootropics", CarbsG: 40, Texture: ProductTexture.Gel, HasCaffeine: false, CaffeineMg: 0),
        new("Energy Bar", CarbsG: 40, Texture: ProductTexture.Bake, HasCaffeine: false, CaffeineMg: 0),
        new("Electrolyte Drink", CarbsG: 15, Texture: ProductTexture.Drink, HasCaffeine: false, CaffeineMg: 0, VolumeMl: 100, ProductType: "Electrolyte"),
        new("Energy Drink", CarbsG: 30, Texture: ProductTexture.Drink, HasCaffeine: false, CaffeineMg: 0, VolumeMl: 500, ProductType: "Energy"),
        new("Chew Mix", CarbsG: 22, Texture: ProductTexture.Chew, HasCaffeine: false, CaffeineMg: 0)
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratePlan_Run4hEasyMixedProducts_SatisfiesAllHardRules(bool caffeineEnabled)
    {
        var generator = new PlanGenerator();
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 4.0,
            Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Easy);
        var products = MixedProducts();

        var result = generator.GeneratePlanWithDiagnostics(race, athlete, products, caffeineEnabled);
        var plan = result.Events;

        _output.WriteLine($"Total events: {plan.Count}, total carbs: {plan.Sum(e => e.CarbsInEvent):F1}g");
        foreach (var e in plan)
            _output.WriteLine($"  t={e.TimeMin,4} [{e.Phase}] {e.Kind,-7} {e.Action,-7} {e.ProductName,-26} c={e.CarbsInEvent,5:F1} sip={e.SipMl}");

        plan.ShouldNotBeEmpty();
        var inRace = plan.Where(e => e.TimeMin > 0).ToList();
        inRace.ShouldNotBeEmpty();
        int duration = PlanInvariants.ToMinutes(race.DurationHours);
        (inRace.Max(e => e.TimeMin) >= (int)(duration * SchedulingConstraints.MinCoverageRatio)).ShouldBeTrue(
            "Plan must cover at least MinCoverageRatio of the race");

        // Bars are bike-only; this is a run.
        plan.ShouldNotContain(e => e.ProductName == "Energy Bar" && e.Kind == EventKind.Intake);

        PlanInvariants.AssertAllHardRules(result, products, duration, caffeineEnabled);
    }
}
