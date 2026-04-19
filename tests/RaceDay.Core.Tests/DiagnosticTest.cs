namespace RaceDay.Core.Tests;
using RaceDay.Core.Services;
using RaceDay.Core.Models;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

public class DiagnosticTest
{
    private readonly ITestOutputHelper _output;
    public DiagnosticTest(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Diag_Run4h_Easy()
    {
        var generator = new PlanGenerator();
        var athlete = new AthleteProfile(WeightKg: 75);
        var race = new RaceProfile(SportType.Run, DurationHours: 4.0,
            Temperature: TemperatureCondition.Moderate, Intensity: IntensityLevel.Easy);
        var products = new List<ProductEnhanced>
        {
            new("Gel Light", CarbsG: 20, Texture: ProductTexture.LightGel, HasCaffeine: false, CaffeineMg: 0),
            new("SiS Beta Fuel Gel", CarbsG: 40, Texture: ProductTexture.Gel, HasCaffeine: true, CaffeineMg: 75),
            new("SiS Beta Fuel Nootropics", CarbsG: 40, Texture: ProductTexture.Gel, HasCaffeine: false, CaffeineMg: 0),
            new("Energy Bar", CarbsG: 40, Texture: ProductTexture.Bake, HasCaffeine: false, CaffeineMg: 0),
            new("Electrolyte Drink", CarbsG: 15, Texture: ProductTexture.Drink, HasCaffeine: false, CaffeineMg: 0, VolumeMl: 100, ProductType: "Electrolyte"),
            new("Energy Drink", CarbsG: 30, Texture: ProductTexture.Drink, HasCaffeine: false, CaffeineMg: 0, VolumeMl: 500, ProductType: "Energy"),
            new("Chew Mix", CarbsG: 22, Texture: ProductTexture.Chew, HasCaffeine: false, CaffeineMg: 0)
        };
        var plan = generator.GeneratePlan(race, athlete, products);
        plan.Sort((a, b) => a.TimeMin.CompareTo(b.TimeMin));
        _output.WriteLine($"Total events: {plan.Count}");
        _output.WriteLine($"Total carbs: {plan.Sum(e => e.CarbsInEvent):F1}");
        _output.WriteLine($"Max TimeMin (>0): {plan.Where(e => e.TimeMin > 0).Max(e => e.TimeMin)}");
        foreach (var e in plan)
            _output.WriteLine($"  t={e.TimeMin,4} [{e.Phase}] {e.Action,-7} {e.ProductName,-30} c={e.CarbsInEvent,5:F1} sip={e.SipMl}");
    }
}
