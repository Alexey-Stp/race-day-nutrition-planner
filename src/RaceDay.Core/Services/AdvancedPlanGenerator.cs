namespace RaceDay.Core.Services;

using RaceDay.Core.Models;
using RaceDay.Core.Exceptions;

/// <summary>
/// Advanced nutrition plan generator with phase tracking, caffeine management,
/// and triathlon-specific rules
/// </summary>
public static class AdvancedPlanGenerator
{
    /// <summary>
    /// Generates a complete nutrition plan with advanced features
    /// </summary>
    public static RaceNutritionPlan GenerateAdvanced(
        RaceMode raceMode,
        double durationHours,
        TemperatureCondition temperature,
        IntensityLevel intensity,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 20)
    {
        var race = new RaceProfile(MapRaceModeToSportType(raceMode), durationHours, temperature, intensity);
        
        // Calculate targets
        var targets = NutritionCalculator.CalculateTargets(race, athlete);
        
        // Generate schedule with phase awareness
        var schedule = GenerateScheduleWithPhases(raceMode, durationHours, targets, products, athlete.WeightKg, intervalMin);
        
        // Calculate totals
        double totalCarbs = 0, totalFluids = 0, totalSodium = 0;
        foreach (var item in schedule.Where(i => i.TimeMin >= 0 && i.Product != null))
        {
            totalCarbs += item.AmountPortions * item.Product!.CarbsG;
            totalFluids += item.AmountPortions * item.Product!.VolumeMl;
            totalSodium += item.AmountPortions * item.Product!.SodiumMg;
        }

        // For running, account for water from race points
        if (raceMode == RaceMode.Running)
        {
            totalFluids = targets.FluidsMlPerHour * durationHours;
        }

        // Calculate product summaries
        var productSummaries = schedule
            .Where(i => i.TimeMin >= 0)
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

    private static SportType MapRaceModeToSportType(RaceMode mode)
    {
        return mode switch
        {
            RaceMode.Running => SportType.Run,
            RaceMode.Cycling => SportType.Bike,
            RaceMode.TriathlonHalf or RaceMode.TriathlonFull => SportType.Triathlon,
            _ => SportType.Run
        };
    }

    private static List<IntakeItem> GenerateScheduleWithPhases(
        RaceMode raceMode,
        double durationHours,
        NutritionTargets targets,
        List<Product> products,
        double weightKg,
        int intervalMin)
    {
        var schedule = new List<IntakeItem>();
        int totalMinutes = (int)(durationHours * 60);
        
        // Calculate phase durations for triathlon
        var phases = CalculatePhases(raceMode, totalMinutes);
        
        double totalCarbsSoFar = 0;
        double totalCaffeineMg = 0;
        int lastCaffeineTimeMin = int.MinValue;
        double maxCaffeineMg = NutritionConfig.MaxCaffeineMgPerKg * weightKg;
        double startCaffeineTimeMin = NutritionConfig.GetStartCaffeineHour(raceMode) * 60;

        // Generate intake schedule
        for (int timeMin = 0; timeMin < totalMinutes; timeMin += intervalMin)
        {
            var currentPhase = GetPhaseAtTime(phases, timeMin);
            
            // Skip swim phase (no nutrition during swim)
            if (currentPhase == RacePhase.Swim)
                continue;

            // Determine which products to use based on phase and time
            var availableProducts = FilterProductsForPhaseAndTime(
                products, currentPhase, timeMin, phases, raceMode);

            if (availableProducts.Count == 0)
                continue;

            // Calculate carbs needed at this interval
            double carbsPerInterval = targets.CarbsGPerHour * intervalMin / 60.0;
            
            // Select products for this interval
            foreach (var product in availableProducts)
            {
                // Check caffeine constraints
                if (product.HasCaffeine)
                {
                    // Too early for caffeine?
                    if (timeMin < startCaffeineTimeMin)
                        continue;
                    
                    // Would exceed max caffeine?
                    if (totalCaffeineMg + product.CaffeineMg > maxCaffeineMg)
                        continue;
                    
                    // Too soon after last caffeine?
                    if (timeMin - lastCaffeineTimeMin < NutritionConfig.MinCaffeineSpacingMin)
                        continue;
                }

                double portions = 0;
                
                // Calculate portions based on product type
                if (product.ProductType == "gel") // TODO: Consider using constants for product types
                {
                    portions = Math.Round((carbsPerInterval / product.CarbsG) * 2) / 2;
                }
                else if (product.ProductType == "drink") // TODO: Consider using constants for product types
                {
                    double fluidsPerInterval = targets.FluidsMlPerHour * intervalMin / 60.0;
                    portions = Math.Round((fluidsPerInterval / product.VolumeMl) * 2) / 2;
                }

                if (portions > 0)
                {
                    totalCarbsSoFar += portions * product.CarbsG;
                    
                    if (product.HasCaffeine)
                    {
                        totalCaffeineMg += portions * product.CaffeineMg;
                        lastCaffeineTimeMin = timeMin;
                    }

                    schedule.Add(new IntakeItem(
                        TimeMin: timeMin,
                        ProductName: product.Name,
                        AmountPortions: portions,
                        Product: product,
                        Phase: currentPhase,
                        TotalCarbsSoFar: totalCarbsSoFar
                    ));
                }
            }
        }

        // Sort by time (should already be sorted, but ensure it)
        return schedule.OrderBy(i => i.TimeMin).ToList();
    }

    private static Dictionary<RacePhase, (int StartMin, int EndMin)> CalculatePhases(RaceMode raceMode, int totalMinutes)
    {
        var phases = new Dictionary<RacePhase, (int, int)>();

        switch (raceMode)
        {
            case RaceMode.Running:
                phases[RacePhase.Run] = (0, totalMinutes);
                break;
            
            case RaceMode.Cycling:
                phases[RacePhase.Bike] = (0, totalMinutes);
                break;
            
            case RaceMode.TriathlonHalf:
                // Half triathlon: ~40min swim, ~3hr bike, ~1.5hr run
                phases[RacePhase.Swim] = (0, 40);
                phases[RacePhase.Bike] = (40, 220);  // 40 + 180 min
                phases[RacePhase.Run] = (220, totalMinutes);
                break;
            
            case RaceMode.TriathlonFull:
                // Full triathlon: ~60min swim, ~6hr bike, ~4hr run
                phases[RacePhase.Swim] = (0, 60);
                phases[RacePhase.Bike] = (60, 420);  // 60 + 360 min
                phases[RacePhase.Run] = (420, totalMinutes);
                break;
        }

        return phases;
    }

    private static RacePhase GetPhaseAtTime(Dictionary<RacePhase, (int StartMin, int EndMin)> phases, int timeMin)
    {
        foreach (var (phase, (start, end)) in phases)
        {
            if (timeMin >= start && timeMin < end)
                return phase;
        }
        return phases.Keys.First(); // Default to first phase
    }

    private static List<Product> FilterProductsForPhaseAndTime(
        List<Product> products,
        RacePhase phase,
        int timeMin,
        Dictionary<RacePhase, (int StartMin, int EndMin)> phases,
        RaceMode raceMode)
    {
        // Filter out any null products from the input list (defensive programming)
        var filtered = products.Where(p => p != null).ToList();

        // Triathlon-specific rules
        if (raceMode == RaceMode.TriathlonHalf || raceMode == RaceMode.TriathlonFull)
        {
            if (phase == RacePhase.Bike)
            {
                var bikeStartMin = phases[RacePhase.Bike].StartMin;
                
                // First 30 minutes of bike: only electrolyte drinks
                if (timeMin < bikeStartMin + 30)
                {
                    filtered = filtered.Where(p => 
                        p.Texture == "Drink" && 
                        p.Type == "Electrolyte").ToList();
                }
            }
            else if (phase == RacePhase.Run)
            {
                var runStartMin = phases[RacePhase.Run].StartMin;
                
                // First hour of run: only light textures
                if (timeMin < runStartMin + 60)
                {
                    filtered = filtered.Where(p => 
                        p.Texture == "LightGel" || 
                        p.Texture == "Bake").ToList();
                }
            }
        }

        // End of race (last 20%): prefer Beta Fuel gels
        var totalMinutes = phases.Values.Max(p => p.EndMin);
        if (timeMin >= 0.8 * totalMinutes)
        {
            var betaFuelGels = filtered.Where(p => 
                p.Texture == "Gel" && 
                p.Name.Contains("Beta Fuel", StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (betaFuelGels.Count > 0)
            {
                // Prefer Beta Fuel but don't exclude others entirely
                filtered = betaFuelGels.Concat(filtered.Where(p => p.ProductType != "gel")).ToList();
            }
        }

        return filtered;
    }
}
