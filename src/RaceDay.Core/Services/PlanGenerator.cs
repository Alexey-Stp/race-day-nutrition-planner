namespace RaceDay.Core.Services;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;
using RaceDay.Core.Utilities;
using RaceDay.Core.Exceptions;

/// <summary>
/// Generates race nutrition plans with time-based schedules
/// </summary>
public static class PlanGenerator
{
    /// <summary>
    /// Generates a complete nutrition plan for the race
    /// </summary>
    /// <param name="race">Race profile</param>
    /// <param name="athlete">Athlete profile</param>
    /// <param name="products">Available nutrition products</param>
    /// <param name="intervalMin">Time interval between intakes in minutes (default: 20)</param>
    /// <returns>Complete race nutrition plan with schedule</returns>
    /// <exception cref="MissingProductException">Thrown when required product types are missing</exception>
    /// <exception cref="ValidationException">Thrown when input validation fails</exception>
    public static RaceNutritionPlan Generate(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 20)
    {
        // Validate inputs
        Validation.ValidateRaceProfile(race);
        Validation.ValidateAthleteProfile(athlete);
        Validation.ValidateInterval(intervalMin);

        var gel = products.FirstOrDefault(p => p.ProductType == "gel")
            ?? throw new MissingProductException("gel");
        
        // Drink is required only for non-running sports
        // For running, water is available at race points
        var drink = race.SportType switch
        {
            SportType.Run => null,
            _ => products.FirstOrDefault(p => p.ProductType == "drink")
                ?? throw new MissingProductException("drink")
        };

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

            if (gelPortions > 0)
            {
                schedule.Add(new IntakeItem(time, gel.Name, gelPortions));
                totalCarbs += gelPortions * gel.CarbsG;
                totalSodium += gelPortions * gel.SodiumMg;
            }

            // Only add drinks for non-running sports
            if (drink != null)
            {
                double drinkPortions = Math.Round((fluidsPerInterval / drink.VolumeMl) * 2) / 2;
                if (drinkPortions > 0)
                {
                    schedule.Add(new IntakeItem(time, drink.Name, drinkPortions));
                    totalFluids += drinkPortions * drink.VolumeMl;
                    totalSodium += drinkPortions * drink.SodiumMg;
                }
            }
        }

        // For running, account for water from race points
        if (race.SportType == SportType.Run)
        {
            totalFluids = targets.FluidsMlPerHour * race.DurationHours;
        }

        // Calculate product summaries for shopping list
        var productSummaries = schedule
            .GroupBy(item => item.ProductName)
            .Select(group => new ProductSummary(
                ProductName: group.Key,
                TotalPortions: group.Sum(item => item.AmountPortions)
            ))
            .OrderBy(summary => summary.ProductName)
            .ToList();

        return new RaceNutritionPlan(
            race,
            targets,
            schedule,
            totalCarbs,
            totalFluids,
            totalSodium,
            productSummaries
        );
    }
}

