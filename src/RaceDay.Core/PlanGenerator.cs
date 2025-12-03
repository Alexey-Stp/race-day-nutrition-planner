namespace RaceDay.Core;

public static class PlanGenerator
{
    public static RaceNutritionPlan Generate(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 20)
    {
        var gel = products.FirstOrDefault(p => p.ProductType == "gel")
            ?? throw new Exception("Gel product not found.");
        var drink = products.FirstOrDefault(p => p.ProductType == "drink")
            ?? throw new Exception("Drink product not found.");

        var targets = NutritionCalculator.CalculateTargets(race, athlete);

        int totalMinutes = (int)(race.DurationHours * 60);
        int intervalsCount = (int)Math.Ceiling((double)totalMinutes / intervalMin);

        double carbsPerInterval = targets.CarbsGPerHour * intervalMin / 60.0;
        double fluidsPerInterval = targets.FluidsMlPerHour * intervalMin / 60.0;

        var schedule = new List<IntakeItem>();
        double totalCarbs = 0, totalFluids = 0, totalSodium = 0;

        for (int i = 0; i < intervalsCount; i++)
        {
            int time = i * intervalMin;

            double gelPortions = Math.Round((carbsPerInterval / gel.CarbsG) * 2) / 2;
            double drinkPortions = Math.Round((fluidsPerInterval / drink.VolumeMl) * 2) / 2;

            if (gelPortions > 0)
            {
                schedule.Add(new IntakeItem(time, gel.Name, gelPortions));
                totalCarbs += gelPortions * gel.CarbsG;
                totalSodium += gelPortions * gel.SodiumMg;
            }

            if (drinkPortions > 0)
            {
                schedule.Add(new IntakeItem(time, drink.Name, drinkPortions));
                totalFluids += drinkPortions * drink.VolumeMl;
                totalSodium += drinkPortions * drink.SodiumMg;
            }
        }

        return new RaceNutritionPlan(
            race,
            targets,
            schedule,
            totalCarbs,
            totalFluids,
            totalSodium
        );
    }
}
