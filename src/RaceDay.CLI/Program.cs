using RaceDay.Core;

var athlete = new AthleteProfile(WeightKg: 89);

var race = new RaceProfile(
    SportType.Triathlon,
    DurationHours: 4.5,
    TemperatureC: 20,
    Intensity: IntensityLevel.Moderate
);

var products = new List<Product>
{
    new Product("Maurten Gel", "gel", CarbsG: 25, SodiumMg: 100),
    new Product("Isotonic Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
};

var plan = PlanGenerator.Generate(race, athlete, products);

Console.WriteLine("=== Race Day Nutrition Plan ===");
Console.WriteLine($"Carbs/h:  {plan.Targets.CarbsGPerHour} g");
Console.WriteLine($"Fluids/h: {plan.Targets.FluidsMlPerHour} ml");
Console.WriteLine($"Sodium/h: {plan.Targets.SodiumMgPerHour} mg");
Console.WriteLine();

foreach (var item in plan.Schedule)
{
    Console.WriteLine($"t = {item.TimeMin,3} min → {item.AmountPortions}× {item.ProductName}");
}

Console.WriteLine();
Console.WriteLine($"Total carbs:  {plan.TotalCarbsG} g");
Console.WriteLine($"Total fluids: {plan.TotalFluidsMl / 1000:F2} L");
Console.WriteLine($"Total sodium: {plan.TotalSodiumMg} mg");