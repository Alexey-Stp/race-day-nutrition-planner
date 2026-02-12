namespace RaceDay.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using RaceDay.Core.Models;
using RaceDay.Core.Constants;

/// <summary>
/// Advanced nutrition planner with sport-specific logic, caffeine strategy, and phase awareness
/// </summary>
public class PlanGenerator
{
    private sealed record PhaseSegment(RacePhase Phase, double StartHour, double EndHour);
    private sealed record Slot(int TimeMin, RacePhase Phase);

    /// <summary>
    /// Planner state tracking during generation
    /// </summary>
    private sealed class PlannerState
    {
        public double TotalCarbs { get; set; }
        public double TotalSodium { get; set; }
        public double TotalFluid { get; set; }
        public double TotalCaffeineMg { get; set; }
        public double NextCaffeineHour { get; set; }
        public int TotalIntakes { get; set; }
        public Queue<string> LastProductNames { get; set; } = new();
        
        public void RecordProduct(string productName)
        {
            LastProductNames.Enqueue(productName);
            if (LastProductNames.Count > 5)
                LastProductNames.Dequeue();
            TotalIntakes++;
        }
        
        public int GetConsecutiveUseCount(string productName)
        {
            return LastProductNames.Reverse().TakeWhile(p => p == productName).Count();
        }
    }

    /// <summary>
    /// Generate an advanced nutrition plan based on race characteristics
    /// </summary>
    public List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        List<ProductEnhanced> products,
        int intervalMinutes = 22,
        bool caffeineEnabled = false)
    {
        var raceMode = DetermineRaceMode(race.SportType);
        var slotInterval = GetSlotInterval(raceMode);
        var durationHours = race.DurationHours;
        var durationMinutes = (int)(durationHours * 60);
        var weightKg = athlete.WeightKg;

        // Build phase timeline (important for triathlon)
        var phases = PlanGenerator.BuildPhaseTimeline(race.SportType, durationHours);
        var slots = BuildSlots(durationMinutes, slotInterval, phases);

        // Calculate multi-nutrient targets
        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled);
        var totalCarbs = targets.CarbsG;

        // Initialize planner state
        var state = InitPlannerState(raceMode, weightKg);
        // Use fixed seed for reproducible results. Safe here as Random is local to this method call
        // and not shared across threads. Each API request gets its own instance.
        var random = new Random(42);

        var plan = new List<NutritionEvent>();

        // Get the main race phase for pre-race event
        var mainPhase = phases.FirstOrDefault()?.Phase ?? RacePhase.Run;

        // === PHASE 1: Build Drink Backbone ===
        // Priority: Use high-carb drinks for efficiency (especially on bike)
        BuildDrinkBackbone(plan, state, products, phases, targets, durationMinutes);

        // Pre-race intake (15 min before) - only if not already covered by drink backbone
        // Never use caffeine pre-race (save for strategic windows during race)
        if (state.TotalCarbs < targets.CarbsG * 0.1) // Less than 10% of target
        {
            // Prefer bars for pre-race, then gels (no caffeine)
            var preRaceCandidates = products.Where(p => !p.HasCaffeine).ToList();
            var preRaceProduct = preRaceCandidates.FirstOrDefault(p => p.Texture == ProductTexture.Bake) 
                              ?? preRaceCandidates.FirstOrDefault(p => p.Texture == ProductTexture.Gel);
            if (preRaceProduct != null)
            {
                state.TotalCarbs += preRaceProduct.CarbsG;
                state.TotalSodium += preRaceProduct.SodiumMg;
                state.RecordProduct(preRaceProduct.Name);
                plan.Add(new NutritionEvent(
                    TimeMin: -15,
                    Phase: mainPhase,
                    PhaseDescription: GetPhaseDescription(mainPhase),
                    ProductName: preRaceProduct.Name,
                    AmountPortions: 1,
                    Action: GetAction(preRaceProduct.Texture),
                    TotalCarbsSoFar: state.TotalCarbs,
                    HasCaffeine: preRaceProduct.HasCaffeine,
                    CaffeineMg: preRaceProduct.HasCaffeine ? preRaceProduct.CaffeineMg : null,
                    TotalCaffeineSoFar: state.TotalCaffeineMg
                ));
            }
        }

        // === PHASE 2: Fill Remaining Slots with Scored Selection ===
        foreach (var slot in slots)
        {
            // Skip eating during swim
            if (slot.Phase == RacePhase.Swim)
                continue;

            // Check if slot already has nutrition from drink backbone
            bool slotOccupied = plan.Any(e => Math.Abs(e.TimeMin - slot.TimeMin) < SchedulingConstraints.ClusterWindow);
            if (slotOccupied)
                continue;

            // Calculate remaining needs
            double currentHour = slot.TimeMin / 60.0;
            double raceProgress = currentHour / durationHours;
            
            var remainingNeeds = new MultiNutrientTargets(
                CarbsG: Math.Max(0, targets.CarbsG - state.TotalCarbs),
                SodiumMg: Math.Max(0, targets.SodiumMg - state.TotalSodium),
                FluidMl: Math.Max(0, targets.FluidMl - state.TotalFluid),
                CaffeineMg: caffeineEnabled ? Math.Max(0, targets.CaffeineMg - state.TotalCaffeineMg) : 0,
                CarbsPerHour: targets.CarbsPerHour,
                SodiumPerHour: targets.SodiumPerHour,
                FluidPerHour: targets.FluidPerHour
            );

            // Stop adding if carbs target met
            if (remainingNeeds.CarbsG < 10)
                break;

            // Select best product using scoring system
            var product = SelectBestProduct(products, slot.Phase, remainingNeeds, state, raceProgress, caffeineEnabled);
            if (product == null)
                continue;

            // Update state
            state.TotalCarbs += product.CarbsG;
            state.TotalSodium += product.SodiumMg;
            state.TotalFluid += product.VolumeMl;
            state.RecordProduct(product.Name);
            
            if (product.HasCaffeine)
            {
                state.TotalCaffeineMg += product.CaffeineMg;
                state.NextCaffeineHour = currentHour + AdvancedNutritionConfig.CaffeineIntervalHours;
            }

            plan.Add(new NutritionEvent(
                TimeMin: slot.TimeMin,
                Phase: slot.Phase,
                PhaseDescription: GetPhaseDescription(slot.Phase),
                ProductName: product.Name,
                AmountPortions: 1,
                Action: GetAction(product.Texture),
                TotalCarbsSoFar: state.TotalCarbs,
                HasCaffeine: product.HasCaffeine,
                CaffeineMg: product.HasCaffeine ? product.CaffeineMg : null,
                TotalCaffeineSoFar: state.TotalCaffeineMg
            ));
        }

        // Add extra products if under target (but not in last 5 minutes)
        // Add a small buffer (5g) to ensure we meet the target accounting for product granularity  
        int safetyMarginMinutes = 5;
        int maxExtraTimeMin = durationMinutes - safetyMarginMinutes;
        var finalPhase = phases.LastOrDefault()?.Phase ?? (raceMode == RaceMode.Cycling ? RacePhase.Bike : RacePhase.Run);
        double targetWithBuffer = totalCarbs + 5; // Add 5g buffer to ensure we exceed target
        
        // Detect triathlon and implement bike-heavy distribution
        var isTriathlon = phases.Any(p => p.Phase == RacePhase.Bike) && phases.Any(p => p.Phase == RacePhase.Run);
        
        if (isTriathlon)
        {
            // For triathlon: 70% carbs on bike, 30% on run
            var bikePhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Bike);
            var runPhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Run);
            
            if (bikePhase != null && runPhase != null)
            {
                double remainingCarbs = targetWithBuffer - state.TotalCarbs;
                double bikeTargetCarbs = state.TotalCarbs + (remainingCarbs * AdvancedNutritionConfig.TriathlonBikeCarbsRatio);
                
                // Phase 1: Fill bike phase first
                int bikeEndMin = (int)(bikePhase.EndHour * 60) - AdvancedNutritionConfig.BikeToRunTransitionMarginMin;
                int bikeStartMin = (int)(bikePhase.StartHour * 60);
                
                while (state.TotalCarbs < bikeTargetCarbs && state.TotalCarbs < targetWithBuffer)
                {
                    var extraProduct = SelectExtraProduct(raceMode, products);
                    if (extraProduct == null) break;
                    
                    var existingBikeTimes = plan.Where(e => e.Phase == RacePhase.Bike)
                        .Select(e => e.TimeMin).OrderBy(t => t).ToList();
                    
                    int? timeSlot = FindAvailableTimeSlot(bikeStartMin, bikeEndMin, existingBikeTimes, 15);
                    if (!timeSlot.HasValue) break;
                    
                    state.TotalCarbs += extraProduct.CarbsG;
                    if (extraProduct.HasCaffeine)
                    {
                        state.TotalCaffeineMg += extraProduct.CaffeineMg;
                    }
                    plan.Add(new NutritionEvent(
                        TimeMin: timeSlot.Value,
                        Phase: RacePhase.Bike,
                        PhaseDescription: GetPhaseDescription(RacePhase.Bike),
                        ProductName: extraProduct.Name,
                        AmountPortions: 1,
                        Action: GetAction(extraProduct.Texture),
                        TotalCarbsSoFar: state.TotalCarbs,
                        HasCaffeine: extraProduct.HasCaffeine,
                        CaffeineMg: extraProduct.HasCaffeine ? extraProduct.CaffeineMg : null,
                        TotalCaffeineSoFar: state.TotalCaffeineMg
                    ));
                }
                
                // Phase 2: Fill run phase with remaining
                int runStartMin = (int)(runPhase.StartHour * 60);
                int runEndMin = Math.Min((int)(runPhase.EndHour * 60), maxExtraTimeMin);
                
                while (state.TotalCarbs < targetWithBuffer)
                {
                    var extraProduct = SelectExtraProduct(raceMode, products);
                    if (extraProduct == null) break;
                    
                    var existingRunTimes = plan.Where(e => e.Phase == RacePhase.Run)
                        .Select(e => e.TimeMin).OrderBy(t => t).ToList();
                    
                    int? timeSlot = FindAvailableTimeSlot(runStartMin, runEndMin, existingRunTimes, 20);
                    if (!timeSlot.HasValue) break;
                    
                    state.TotalCarbs += extraProduct.CarbsG;
                    if (extraProduct.HasCaffeine)
                    {
                        state.TotalCaffeineMg += extraProduct.CaffeineMg;
                    }
                    plan.Add(new NutritionEvent(
                        TimeMin: timeSlot.Value,
                        Phase: RacePhase.Run,
                        PhaseDescription: GetPhaseDescription(RacePhase.Run),
                        ProductName: extraProduct.Name,
                        AmountPortions: 1,
                        Action: GetAction(extraProduct.Texture),
                        TotalCarbsSoFar: state.TotalCarbs,
                        HasCaffeine: extraProduct.HasCaffeine,
                        CaffeineMg: extraProduct.HasCaffeine ? extraProduct.CaffeineMg : null,
                        TotalCaffeineSoFar: state.TotalCaffeineMg
                    ));
                }
            }
        }
        else
        {
            // For non-triathlon, use simple countdown approach that always works
            int extraProductCount = 0;
            
            while (state.TotalCarbs < targetWithBuffer && extraProductCount < 30)
            {
                var extraProduct = SelectExtraProduct(raceMode, products);
                if (extraProduct == null) break;
                
                // Simple countdown from end: works every time
                var existingTimes = plan.Select(e => e.TimeMin).ToHashSet();
                int timeSlot = maxExtraTimeMin - (extraProductCount * 10);
                
                // Ensure positive and avoid duplicates
                if (timeSlot < 5)
                    timeSlot = maxExtraTimeMin - (extraProductCount % 10);
                    
                // Find first unused slot
                int attempts = 0;
                while (existingTimes.Contains(timeSlot) && attempts < 200)
                {
                    timeSlot--;
                    if (timeSlot < 0) timeSlot = maxExtraTimeMin - attempts;
                    attempts++;
                }
                
                if (timeSlot < 0 || timeSlot >= durationMinutes) break;
                
                state.TotalCarbs += extraProduct.CarbsG;
                if (extraProduct.HasCaffeine)
                {
                    state.TotalCaffeineMg += extraProduct.CaffeineMg;
                }
                plan.Add(new NutritionEvent(
                    TimeMin: timeSlot,
                    Phase: finalPhase,
                    PhaseDescription: GetPhaseDescription(finalPhase),
                    ProductName: extraProduct.Name,
                    AmountPortions: 1,
                    Action: GetAction(extraProduct.Texture),
                    TotalCarbsSoFar: state.TotalCarbs,
                    HasCaffeine: extraProduct.HasCaffeine,
                    CaffeineMg: extraProduct.HasCaffeine ? extraProduct.CaffeineMg : null,
                    TotalCaffeineSoFar: state.TotalCaffeineMg
                ));
                
                extraProductCount++;
            }
        }
        
        // Sort by time and recalculate cumulative carbs and caffeine
        plan = plan.OrderBy(e => e.TimeMin).ToList();
        double cumulativeCarbs = 0;
        double cumulativeCaffeine = 0;
        for (int i = 0; i < plan.Count; i++)
        {
            var evt = plan[i];
            var product = products.FirstOrDefault(p => p.Name == evt.ProductName);
            cumulativeCarbs += product?.CarbsG ?? 0;
            if (product?.HasCaffeine == true)
            {
                cumulativeCaffeine += product.CaffeineMg;
            }
            plan[i] = evt with { TotalCarbsSoFar = cumulativeCarbs, TotalCaffeineSoFar = cumulativeCaffeine };
        }
        
        // Final check: if still under target after recalculation, add one more product
        if (cumulativeCarbs < totalCarbs)
        {
            var lastExtraProduct = SelectExtraProduct(raceMode, products);
            if (lastExtraProduct != null)
            {
                int lastTimeSlot = maxExtraTimeMin;
                var existingTimes = plan.Select(e => e.TimeMin).ToHashSet();
                while (existingTimes.Contains(lastTimeSlot) && lastTimeSlot > 0)
                {
                    lastTimeSlot--;
                }
                
                cumulativeCarbs += lastExtraProduct.CarbsG;
                if (lastExtraProduct.HasCaffeine)
                {
                    cumulativeCaffeine += lastExtraProduct.CaffeineMg;
                }
                plan.Add(new NutritionEvent(
                    TimeMin: lastTimeSlot,
                    Phase: finalPhase,
                    PhaseDescription: GetPhaseDescription(finalPhase),
                    ProductName: lastExtraProduct.Name,
                    AmountPortions: 1,
                    Action: GetAction(lastExtraProduct.Texture),
                    TotalCarbsSoFar: cumulativeCarbs,
                    HasCaffeine: lastExtraProduct.HasCaffeine,
                    CaffeineMg: lastExtraProduct.HasCaffeine ? lastExtraProduct.CaffeineMg : null,
                    TotalCaffeineSoFar: cumulativeCaffeine
                ));
                
                // Re-sort and recalculate one more time
                plan = plan.OrderBy(e => e.TimeMin).ToList();
                cumulativeCarbs = 0;
                cumulativeCaffeine = 0;
                for (int i = 0; i < plan.Count; i++)
                {
                    var evt = plan[i];
                    var product = products.FirstOrDefault(p => p.Name == evt.ProductName);
                    cumulativeCarbs += product?.CarbsG ?? 0;
                    if (product?.HasCaffeine == true)
                    {
                        cumulativeCaffeine += product.CaffeineMg;
                    }
                    plan[i] = evt with { TotalCarbsSoFar = cumulativeCarbs, TotalCaffeineSoFar = cumulativeCaffeine };
                }
            }
        }

        // === PHASE 3: Validate and Auto-Fix ===
        var validationResult = ValidateAndAutoFix(plan, targets, products, durationMinutes, caffeineEnabled);
        
        // TODO: Surface warnings/errors to API response
        // For now, just return the validated plan
        return validationResult.Plan;
    }

    /// <summary>
    /// Generate plan with full diagnostics (warnings, errors)
    /// </summary>
    public PlanResult GeneratePlanWithDiagnostics(
        RaceProfile race,
        AthleteProfile athlete,
        List<ProductEnhanced> products,
        int intervalMinutes = 22,
        bool caffeineEnabled = false)
    {
        var plan = GeneratePlan(race, athlete, products, intervalMinutes, caffeineEnabled);
        
        // Re-run validation to capture warnings/errors
        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled);
        var durationMinutes = (int)(race.DurationHours * 60);
        var validationResult = ValidateAndAutoFix(plan, targets, products, durationMinutes, caffeineEnabled);
        
        return new PlanResult(
            validationResult.Plan,
            validationResult.Warnings,
            validationResult.Errors
        );
    }

    /// <summary>
    /// Build drink backbone - prioritize high-carb drinks for efficiency
    /// </summary>
    private static void BuildDrinkBackbone(
        List<NutritionEvent> plan,
        PlannerState state,
        List<ProductEnhanced> products,
        List<PhaseSegment> phases,
        MultiNutrientTargets targets,
        int durationMinutes)
    {
        // Find high-carb drinks (>30g carbs)
        var highCarbDrinks = products
            .Where(p => p.Texture == ProductTexture.Drink && p.CarbsG > 30)
            .OrderByDescending(p => p.CarbsG)
            .ToList();

        if (!highCarbDrinks.Any())
            return;

        // Target: ~40-50% of carbs from drinks for efficiency
        double drinkTargetCarbs = targets.CarbsG * 0.45;
        
        // For triathlon, focus drinks on bike phase
        var bikePhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Bike);
        if (bikePhase != null)
        {
            int bikeStartMin = (int)(bikePhase.StartHour * 60);
            int bikeEndMin = (int)(bikePhase.EndHour * 60) - AdvancedNutritionConfig.BikeToRunTransitionMarginMin;
            
            // Schedule drinks every 30-40 minutes on bike
            int drinkInterval = 35;
            for (int timeMin = bikeStartMin + 15; timeMin < bikeEndMin && state.TotalCarbs < drinkTargetCarbs; timeMin += drinkInterval)
            {
                var drink = highCarbDrinks.First();
                state.TotalCarbs += drink.CarbsG;
                state.TotalSodium += drink.SodiumMg;
                state.TotalFluid += drink.VolumeMl;
                if (drink.HasCaffeine)
                {
                    state.TotalCaffeineMg += drink.CaffeineMg;
                }
                state.RecordProduct(drink.Name);
                
                plan.Add(new NutritionEvent(
                    TimeMin: timeMin,
                    Phase: RacePhase.Bike,
                    PhaseDescription: GetPhaseDescription(RacePhase.Bike),
                    ProductName: drink.Name,
                    AmountPortions: 1,
                    Action: "Drink",
                    TotalCarbsSoFar: state.TotalCarbs,
                    HasCaffeine: drink.HasCaffeine,
                    CaffeineMg: drink.HasCaffeine ? drink.CaffeineMg : null,
                    TotalCaffeineSoFar: state.TotalCaffeineMg
                ));
            }
        }
        else
        {
            // For non-triathlon, spread drinks throughout race
            int drinkInterval = 40;
            for (int timeMin = 20; timeMin < durationMinutes - 10 && state.TotalCarbs < drinkTargetCarbs; timeMin += drinkInterval)
            {
                var drink = highCarbDrinks.First();
                double hour = timeMin / 60.0;
                var phaseSegment = phases.FirstOrDefault(p => hour >= p.StartHour && hour < p.EndHour);
                // Handle race end boundary
                if (phaseSegment == null && phases.Any())
                {
                    var lastPhase = phases.Last();
                    if (hour >= lastPhase.StartHour && hour <= lastPhase.EndHour)
                    {
                        phaseSegment = lastPhase;
                    }
                }
                var phase = phaseSegment?.Phase ?? RacePhase.Run;
                
                state.TotalCarbs += drink.CarbsG;
                state.TotalSodium += drink.SodiumMg;
                state.TotalFluid += drink.VolumeMl;
                if (drink.HasCaffeine)
                {
                    state.TotalCaffeineMg += drink.CaffeineMg;
                }
                state.RecordProduct(drink.Name);
                
                plan.Add(new NutritionEvent(
                    TimeMin: timeMin,
                    Phase: phase,
                    PhaseDescription: GetPhaseDescription(phase),
                    ProductName: drink.Name,
                    AmountPortions: 1,
                    Action: "Drink",
                    TotalCarbsSoFar: state.TotalCarbs,
                    HasCaffeine: drink.HasCaffeine,
                    CaffeineMg: drink.HasCaffeine ? drink.CaffeineMg : null,
                    TotalCaffeineSoFar: state.TotalCaffeineMg
                ));
            }
        }
    }

    /// <summary>
    /// Select best product using scoring system
    /// </summary>
    private static ProductEnhanced? SelectBestProduct(
        List<ProductEnhanced> products,
        RacePhase segment,
        MultiNutrientTargets remainingNeeds,
        PlannerState state,
        double raceProgressPercent,
        bool caffeineEnabled)
    {
        // Determine if caffeine is allowed at current race progress
        bool caffeineAllowedNow = caffeineEnabled && raceProgressPercent >= SchedulingConstraints.CaffeinePreferredStartPercent;
        
        var candidates = products
            .Where(p => p.Texture != ProductTexture.Bake || segment == RacePhase.Bike) // No bars on run by default
            .Where(p => !p.HasCaffeine || caffeineAllowedNow) // Filter out caffeine if disabled OR too early
            .Select(p => new
            {
                Product = p,
                Score = ScoreProduct(p, segment, remainingNeeds, state, raceProgressPercent)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (!candidates.Any())
            return null;

        // Apply caffeine preference if enabled and appropriate timing
        if (caffeineAllowedNow)
        {
            var caffeineOptions = candidates.Where(c => c.Product.HasCaffeine).ToList();
            if (caffeineOptions.Any() && state.TotalCaffeineMg < remainingNeeds.CaffeineMg)
            {
                // Prefer caffeinated option
                return caffeineOptions.First().Product;
            }
        }

        return candidates.First().Product;
    }

    #region Helper Methods

    private enum RaceMode { Running, Cycling, TriathlonHalf, TriathlonFull }

    /// <summary>
    /// Get human-friendly description for a race phase
    /// </summary>
    private static string GetPhaseDescription(RacePhase phase) =>
        phase switch
        {
            RacePhase.Swim => "Swim - Lower intensity nutrition due to difficulty of consuming during water",
            RacePhase.Bike => "Bike - Optimal for consuming nutrition, easier digestion",
            RacePhase.Run => "Run - Stomach more sensitive, prefer gels and drinks",
            _ => "Race Phase"
        };

    private static RaceMode DetermineRaceMode(SportType sportType) =>
        sportType switch
        {
            SportType.Bike => RaceMode.Cycling,
            SportType.Triathlon => RaceMode.TriathlonHalf, // Detect triathlon properly
            _ => RaceMode.Running
        };

    private static int GetSlotInterval(RaceMode mode) =>
        mode switch
        {
            RaceMode.TriathlonHalf or RaceMode.TriathlonFull => AdvancedNutritionConfig.TriathlonSlotIntervalMin,
            RaceMode.Cycling => AdvancedNutritionConfig.CyclingSlotIntervalMin,
            _ => AdvancedNutritionConfig.RunningSlotIntervalMin
        };

    private static double CalculateCarbsPerHour(RaceMode mode, double weightKg)
    {
        double perKg = mode switch
        {
            RaceMode.TriathlonHalf or RaceMode.TriathlonFull => AdvancedNutritionConfig.TriathlonCarbsPerKgPerHour,
            RaceMode.Cycling => AdvancedNutritionConfig.CyclingCarbsPerKgPerHour,
            _ => AdvancedNutritionConfig.RunningCarbsPerKgPerHour
        };

        double hardCap = mode switch
        {
            RaceMode.TriathlonHalf or RaceMode.TriathlonFull => AdvancedNutritionConfig.MaxTriathlonCarbsPerHour,
            RaceMode.Cycling => AdvancedNutritionConfig.MaxCyclingCarbsPerHour,
            _ => AdvancedNutritionConfig.MaxRunningCarbsPerHour
        };

        return Math.Min(hardCap, perKg * weightKg);
    }

    private static List<PhaseSegment> BuildPhaseTimeline(RaceMode mode, double totalHours)
    {
        var phase = mode == RaceMode.Cycling ? RacePhase.Bike : RacePhase.Run;
        return new List<PhaseSegment> { new(phase, 0, totalHours) };
    }
    
    private static List<PhaseSegment> BuildPhaseTimeline(SportType sportType, double totalHours)
    {
        if (sportType == SportType.Triathlon)
        {
            // Triathlon: Swim (no nutrition) -> Bike -> Run
            // Rough estimates: Swim ~20%, Bike ~50%, Run ~30% of total time
            const double swimPercent = 0.20;
            const double bikePercent = 0.50;
            // runPercent not used in calculations, durations derived from swim+bike
            
            double swimEnd = totalHours * swimPercent;
            double bikeEnd = swimEnd + (totalHours * bikePercent);
            
            return new List<PhaseSegment>
            {
                new(RacePhase.Swim, 0, swimEnd),
                new(RacePhase.Bike, swimEnd, bikeEnd),
                new(RacePhase.Run, bikeEnd, totalHours)
            };
        }
        
        var phase = sportType == SportType.Bike ? RacePhase.Bike : RacePhase.Run;
        return new List<PhaseSegment> { new(phase, 0, totalHours) };
    }

    private static List<Slot> BuildSlots(int durationMinutes, int slotInterval, List<PhaseSegment> phases)
    {
        var slots = new List<Slot>();
        
        // Safety margin: Don't schedule nutrition in the last 5 minutes of race
        int raceSafetyMarginMinutes = 5;
        int maxTimeMin = durationMinutes - raceSafetyMarginMinutes;
        
        // Detect if this is a triathlon (multiple phases)
        bool isTriathlon = phases.Count > 1 && phases.Any(p => p.Phase == RacePhase.Bike);
        
        // For triathlon, add safety margins before transitions
        var transitionMargins = new Dictionary<RacePhase, int>();
        if (isTriathlon)
        {
            // Triathlon: don't fuel in last 10 min of bike before T2
            var bikePhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Bike);
            if (bikePhase != null)
            {
                var bikeEndMin = (int)(bikePhase.EndHour * 60);
                transitionMargins[RacePhase.Bike] = bikeEndMin - AdvancedNutritionConfig.BikeToRunTransitionMarginMin;
            }
        }

        // Build slots with phase-specific intervals for triathlon
        int currentTime = slotInterval; // Start at first interval
        
        while (currentTime < maxTimeMin)
        {
            double hour = currentTime / 60.0;
            var currentPhase = phases
                .FirstOrDefault(p => hour >= p.StartHour && hour < p.EndHour);
            
            // Handle race end boundary - if at or past last phase end, use last phase
            if (currentPhase == null && phases.Any())
            {
                var lastPhase = phases.Last();
                if (hour >= lastPhase.StartHour && hour <= lastPhase.EndHour)
                {
                    currentPhase = lastPhase;
                }
            }
            
            if (currentPhase != null)
            {
                // Check if we're too close to a transition
                if (transitionMargins.ContainsKey(currentPhase.Phase) && 
                    currentTime >= transitionMargins[currentPhase.Phase])
                {
                    // Skip this slot - too close to transition
                    currentTime += slotInterval;
                    continue;
                }
                
                slots.Add(new Slot(currentTime, currentPhase.Phase));
            }
            
            // Use phase-specific intervals for triathlon only
            if (isTriathlon)
            {
                if (currentPhase?.Phase == RacePhase.Bike)
                {
                    currentTime += 18; // More frequent on bike (every 18 min)
                }
                else if (currentPhase?.Phase == RacePhase.Run)
                {
                    currentTime += 25; // Less frequent on run (every 25 min)
                }
                else
                {
                    currentTime += slotInterval;
                }
            }
            else
            {
                // For non-triathlon, use consistent slot interval
                currentTime += slotInterval;
            }
        }

        return slots;
    }

    private PlannerState InitPlannerState(RaceMode mode, double weightKg)
    {
        double startHour = mode switch
        {
            RaceMode.TriathlonHalf or RaceMode.TriathlonFull => AdvancedNutritionConfig.StartCaffeinHourTriathlon,
            RaceMode.Cycling => AdvancedNutritionConfig.StartCaffeinHourCycling,
            _ => AdvancedNutritionConfig.StartCaffeinHourRunning
        };

        return new PlannerState
        {
            TotalCarbs = 0,
            TotalCaffeineMg = 0,
            NextCaffeineHour = startHour
        };
    }

    private static int? FindAvailableTimeSlot(int startMin, int endMin, List<int> existingTimes, int minSpacingMin)
    {
        if (!existingTimes.Any())
            return startMin + 5; // If no existing times, start 5 minutes into the phase
        
        var existingSet = existingTimes.ToHashSet();
        
        // Strategy 1: Try to find a slot between existing times with minimum spacing
        var sortedTimes = existingTimes.OrderBy(t => t).ToList();
        for (int i = 0; i < sortedTimes.Count - 1; i++)
        {
            int gap = sortedTimes[i + 1] - sortedTimes[i];
            if (gap >= minSpacingMin * 2)
            {
                // Found a gap - place in the middle
                int proposedTime = sortedTimes[i] + (gap / 2);
                if (!existingSet.Contains(proposedTime))
                    return proposedTime;
            }
        }
        
        // Strategy 2: Try to add before first existing time
        if (sortedTimes[0] - startMin >= minSpacingMin)
        {
            int proposedTime = Math.Max(startMin + 5, sortedTimes[0] - minSpacingMin);
            if (!existingSet.Contains(proposedTime))
                return proposedTime;
        }
        
        // Strategy 3: Try to add after last existing time
        if (endMin - sortedTimes[^1] >= minSpacingMin)
        {
            int proposedTime = Math.Min(sortedTimes[^1] + minSpacingMin, endMin - 5);
            if (!existingSet.Contains(proposedTime))
                return proposedTime;
        }
        
        // Strategy 4: Find ANY unused time slot (last resort)
        for (int time = startMin; time <= endMin; time++)
        {
            if (!existingSet.Contains(time))
                return time;
        }
        
        return null; // No available slot found
    }

    private static bool ShouldUseCaffeine(double currentHour, double weightKg, PlannerState state)
    {
        double maxTotal = AdvancedNutritionConfig.MaxCaffeineMgPerKg * weightKg;

        if (currentHour < state.NextCaffeineHour)
            return false;

        if (state.TotalCaffeineMg >= maxTotal)
            return false;

        return currentHour >= state.NextCaffeineHour - 0.1;
    }

    private static ProductEnhanced? SelectProductForSlot(
        RaceMode mode,
        bool isEndPhase,
        bool wantsCaffeine,
        List<ProductEnhanced> products,
        Random random)
    {
        var candidates = mode switch
        {
            RaceMode.Cycling => SelectCyclingCandidates(isEndPhase, products, random),
            _ => SelectRunningCandidates(isEndPhase, products)
        };

        if (!candidates.Any())
            return null;

        var selected = candidates.OrderBy(_ => random.Next()).First();

        if (wantsCaffeine && !selected.HasCaffeine)
        {
            var caffeinated = products.FirstOrDefault(p =>
                p.HasCaffeine &&
                (p.Texture == selected.Texture || p.ProductType == selected.ProductType));

            if (caffeinated != null)
                selected = caffeinated;
        }

        return selected;
    }

    private static IEnumerable<ProductEnhanced> SelectCyclingCandidates(
        bool isEndPhase,
        List<ProductEnhanced> products,
        Random random)
    {
        if (!isEndPhase && random.NextDouble() < 0.4)
            return products.Where(p => p.Texture == ProductTexture.Chew || p.Texture == ProductTexture.Bake);

        if (random.Next(0, 3) == 0)
            return products.Where(p => p.Texture == ProductTexture.Drink && p.ProductType == "Energy" && p.VolumeMl >= 500);

        return products.Where(p => p.Texture == ProductTexture.Gel || p.Texture == ProductTexture.LightGel);
    }

    private static IEnumerable<ProductEnhanced> SelectRunningCandidates(
        bool isEndPhase,
        List<ProductEnhanced> products)
    {
        if (!isEndPhase)
            return products.Where(p => p.Texture == ProductTexture.LightGel);

        if (isEndPhase)
            return products.Where(p => p.Texture == ProductTexture.Gel);

        return products.Where(p => p.Texture == ProductTexture.Gel || p.Texture == ProductTexture.LightGel);
    }

    private static ProductEnhanced? SelectExtraProduct(RaceMode mode, List<ProductEnhanced> products) =>
        mode switch
        {
            RaceMode.Cycling => products.FirstOrDefault(p => p.Texture == ProductTexture.Chew || p.Texture == ProductTexture.Bake),
            _ => products.FirstOrDefault(p => p.Texture == ProductTexture.Gel)
        };

    /// <summary>
    /// Score a product for selection priority based on segment, needs, and state
    /// </summary>
    private static double ScoreProduct(
        ProductEnhanced product,
        RacePhase segment,
        MultiNutrientTargets remainingNeeds,
        PlannerState state,
        double raceProgressPercent)
    {
        double score = 0.0;

        // 1. Carb efficiency (carbs per intake action)
        double carbEfficiency = product.CarbsG;
        score += carbEfficiency * 2.0;

        // 2. Segment suitability
        score += GetSegmentSuitabilityScore(product, segment);

        // 3. Sodium contribution (if needed)
        if (remainingNeeds.SodiumMg > 0)
        {
            double sodiumFit = Math.Min(product.SodiumMg / remainingNeeds.SodiumMg, 1.0);
            score += sodiumFit * 15;
        }

        // 4. Caffeine fit with strategic window optimization
        if (remainingNeeds.CaffeineMg > 0 && raceProgressPercent >= SchedulingConstraints.CaffeinePreferredStartPercent)
        {
            if (product.HasCaffeine)
            {
                double caffeineBonus = CalculateCaffeineWindowBonus(raceProgressPercent);
                
                // Optimal dose range: 50-100mg
                if (product.CaffeineMg >= 50 && product.CaffeineMg <= 100)
                {
                    score += 25 + caffeineBonus;
                }
                else if (product.CaffeineMg > 0)
                {
                    score += 10 + (caffeineBonus * 0.5); // Partial bonus for sub-optimal doses
                }
            }
        }

        // 5. Diversity penalty (avoid excessive repetition)
        int consecutiveUses = state.GetConsecutiveUseCount(product.Name);
        if (consecutiveUses >= 2)
        {
            score -= consecutiveUses * 15;
        }

        // 6. Action count penalty (if already high intake frequency)
        double intakesPerHour = state.TotalIntakes / (raceProgressPercent + 0.1); // avoid div by 0
        if (intakesPerHour > SchedulingConstraints.MaxIntakesPerHour)
        {
            score -= 10;
        }

        return score;
    }

    private static double GetSegmentSuitabilityScore(ProductEnhanced product, RacePhase segment)
    {
        return segment switch
        {
            RacePhase.Bike => product.Texture switch
            {
                ProductTexture.Drink when product.CarbsG > 30 => 50, // Strong preference for high-carb drinks
                ProductTexture.Drink => 30,
                ProductTexture.Bake => 20,
                ProductTexture.Chew => 15,
                ProductTexture.Gel => 10,
                ProductTexture.LightGel => 5,
                _ => 0
            },
            RacePhase.Run => product.Texture switch
            {
                ProductTexture.Gel when IsIsotonic(product) => 40, // Isotonic gels ideal for run
                ProductTexture.Gel => 25,
                ProductTexture.LightGel => 20,
                ProductTexture.Drink when product.VolumeMl <= 200 => 15,
                ProductTexture.Drink => 5,
                ProductTexture.Bake => -30, // Strong penalty for solids on run
                ProductTexture.Chew => -10,
                _ => 0
            },
            RacePhase.Swim => product.Texture switch
            {
                ProductTexture.Gel when IsIsotonic(product) => 20,
                ProductTexture.Gel => 10,
                _ => -20 // Generally avoid nutrition during swim
            },
            _ => 0
        };
    }

    private static bool IsIsotonic(ProductEnhanced product)
    {
        // Check if product is isotonic (6-8% carb concentration typical for isotonic)
        // This is a heuristic - ideally this would be a product property
        if (product.VolumeMl > 0)
        {
            double carbPercent = (product.CarbsG / (product.VolumeMl / 1000.0)) / 10.0; // rough estimate
            return carbPercent >= 6 && carbPercent <= 8;
        }
        return product.ProductType?.Contains("isotonic", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Calculate bonus score for caffeine products based on strategic timing windows.
    /// Returns higher bonus when race progress aligns with optimal caffeine windows.
    /// </summary>
    private static double CalculateCaffeineWindowBonus(double raceProgressPercent)
    {
        // Window 1: 40-55% (early strategic boost)
        if (raceProgressPercent >= SchedulingConstraints.CaffeineOptimalWindow1Start && 
            raceProgressPercent <= SchedulingConstraints.CaffeineOptimalWindow1End)
        {
            return 15.0;
        }
        
        // Window 2: 65-80% (mid-late race maintenance)
        if (raceProgressPercent >= SchedulingConstraints.CaffeineOptimalWindow2Start && 
            raceProgressPercent <= SchedulingConstraints.CaffeineOptimalWindow2End)
        {
            return 20.0;
        }
        
        // Window 3: 85-95% (final push)
        if (raceProgressPercent >= SchedulingConstraints.CaffeineOptimalWindow3Start && 
            raceProgressPercent <= SchedulingConstraints.CaffeineOptimalWindow3End)
        {
            return 25.0;
        }
        
        // Outside optimal windows but after 40% - still acceptable
        if (raceProgressPercent >= SchedulingConstraints.CaffeinePreferredStartPercent)
        {
            return 5.0;
        }
        
        // Too early for caffeine
        return 0.0;
    }

    private static string GetAction(ProductTexture texture) =>
        texture switch
        {
            ProductTexture.Gel or ProductTexture.LightGel => "Squeeze",
            ProductTexture.Drink => "Drink",
            ProductTexture.Chew => "Chew",
            ProductTexture.Bake => "Eat",
            _ => "Consume"
        };

    /// <summary>
    /// Validation result containing plan, warnings, and errors
    /// </summary>
    private sealed record ValidationResult(
        List<NutritionEvent> Plan,
        List<string> Warnings,
        List<string> Errors
    );

    /// <summary>
    /// Validate plan and apply auto-fixes where possible
    /// </summary>
    private static ValidationResult ValidateAndAutoFix(
        List<NutritionEvent> plan,
        MultiNutrientTargets targets,
        List<ProductEnhanced> products,
        int durationMinutes,
        bool caffeineEnabled)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        // === Math Consistency ===
        double calculatedCarbs = 0;
        double calculatedSodium = 0;
        double calculatedCaffeine = 0;

        foreach (var evt in plan)
        {
            var product = products.FirstOrDefault(p => p.Name == evt.ProductName);
            if (product != null)
            {
                calculatedCarbs += product.CarbsG;
                calculatedSodium += product.SodiumMg;
                if (product.HasCaffeine)
                    calculatedCaffeine += product.CaffeineMg;
            }
        }

        // === Target Consistency ===
        double carbTolerance = targets.CarbsG * SchedulingConstraints.TargetTolerancePercent;
        if (calculatedCarbs < targets.CarbsG - carbTolerance)
        {
            warnings.Add($"Plan underdelivers carbs: {calculatedCarbs:F0}g < {targets.CarbsG:F0}g target (-{targets.CarbsG - calculatedCarbs:F0}g)");
        }
        else if (calculatedCarbs > targets.CarbsG + carbTolerance)
        {
            warnings.Add($"Plan overdelivers carbs: {calculatedCarbs:F0}g > {targets.CarbsG:F0}g target (+{calculatedCarbs - targets.CarbsG:F0}g)");
            // Could implement TrimExcessCarbs here if needed
        }

        // === Spacing Validation ===
        for (int i = 0; i < plan.Count - 1; i++)
        {
            var evt1 = plan[i];
            var evt2 = plan[i + 1];
            int spacing = evt2.TimeMin - evt1.TimeMin;

            var product1 = products.FirstOrDefault(p => p.Name == evt1.ProductName);
            var product2 = products.FirstOrDefault(p => p.Name == evt2.ProductName);

            if (product1 != null && product2 != null)
            {
                int minSpacing = GetMinimumSpacing(product1, product2, evt1.Phase);
                if (spacing < minSpacing)
                {
                    errors.Add($"Spacing violation: {evt1.ProductName} at {evt1.TimeMin}min and {evt2.ProductName} at {evt2.TimeMin}min (gap={spacing}min, required={minSpacing}min)");
                }
            }

            // Clustering check
            if (spacing < SchedulingConstraints.ClusterWindow)
            {
                errors.Add($"Clustering detected: items at {evt1.TimeMin}min and {evt2.TimeMin}min too close ({spacing}min < {SchedulingConstraints.ClusterWindow}min)");
            }
        }

        // === Caffeine Validation ===
        if (!caffeineEnabled && calculatedCaffeine > 0)
        {
            errors.Add($"Caffeine disabled but plan contains {calculatedCaffeine:F0}mg");
        }
        else if (caffeineEnabled && calculatedCaffeine > targets.CaffeineMg * 1.2)
        {
            warnings.Add($"Caffeine high: {calculatedCaffeine:F0}mg > {targets.CaffeineMg:F0}mg target");
        }

        // === Product Diversity ===
        var productCounts = plan.GroupBy(e => e.ProductName)
                               .ToDictionary(g => g.Key, g => g.Count());
        if (productCounts.Any())
        {
            var mostCommon = productCounts.MaxBy(kvp => kvp.Value);
            if (mostCommon.Value > plan.Count * 0.6)
            {
                warnings.Add($"Low diversity: {mostCommon.Key} used {mostCommon.Value} times ({mostCommon.Value * 100 / plan.Count}% of plan)");
            }
        }

        // === Drink Usage Check ===
        bool hasHighCarbDrinks = products.Any(p => p.Texture == ProductTexture.Drink && p.CarbsG > 30);
        bool planUsesDrinks = plan.Any(e =>
        {
            var prod = products.FirstOrDefault(p => p.Name == e.ProductName);
            return prod != null && prod.Texture == ProductTexture.Drink && prod.CarbsG > 0;
        });

        if (hasHighCarbDrinks && !planUsesDrinks && calculatedCarbs > 200)
        {
            warnings.Add("No carb drinks used despite being available. Consider using drink mixes for efficiency.");
        }

        // === Hydration Coupling Check ===
        var hydrationWarnings = CheckHydrationCoupling(plan, products);
        warnings.AddRange(hydrationWarnings);

        return new ValidationResult(plan, warnings, errors);
    }

    private static int GetMinimumSpacing(ProductEnhanced product1, ProductEnhanced product2, RacePhase phase)
    {
        // Check caffeine spacing first (highest priority)
        if (product1.HasCaffeine || product2.HasCaffeine)
        {
            return SchedulingConstraints.MinCaffeineSpacing;
        }

        // Check solid spacing
        bool isSolid1 = product1.Texture == ProductTexture.Bake || product1.Texture == ProductTexture.Chew;
        bool isSolid2 = product2.Texture == ProductTexture.Bake || product2.Texture == ProductTexture.Chew;

        if (isSolid1 || isSolid2)
        {
            return phase == RacePhase.Bike
                ? SchedulingConstraints.MinSolidSpacingBike
                : SchedulingConstraints.MinSolidSpacingRun;
        }

        // Check gel spacing
        bool isGel1 = product1.Texture == ProductTexture.Gel || product1.Texture == ProductTexture.LightGel;
        bool isGel2 = product2.Texture == ProductTexture.Gel || product2.Texture == ProductTexture.LightGel;

        if (isGel1 || isGel2)
        {
            return phase == RacePhase.Bike
                ? SchedulingConstraints.MinGelSpacingBike
                : SchedulingConstraints.MinGelSpacingRun;
        }

        // Default to drink spacing
        return SchedulingConstraints.MinDrinkSpacing;
    }

    /// <summary>
    /// Check if non-isotonic gels have adequate hydration nearby
    /// </summary>
    private static List<string> CheckHydrationCoupling(List<NutritionEvent> plan, List<ProductEnhanced> products)
    {
        var warnings = new List<string>();
        const int hydrationWindowMin = 10; // Look for hydration within 10 minutes

        for (int i = 0; i < plan.Count; i++)
        {
            var evt = plan[i];
            var product = products.FirstOrDefault(p => p.Name == evt.ProductName);
            
            if (product == null) continue;

            // Check if this is a non-isotonic gel
            bool isNonIsotonicGel = (product.Texture == ProductTexture.Gel || product.Texture == ProductTexture.LightGel)
                                    && !IsIsotonic(product);

            if (!isNonIsotonicGel) continue;

            // Look for hydration (drink with volume) within the window
            bool hasNearbyHydration = false;
            
            for (int j = 0; j < plan.Count; j++)
            {
                if (i == j) continue;
                
                var otherEvt = plan[j];
                int timeDiff = Math.Abs(otherEvt.TimeMin - evt.TimeMin);
                
                if (timeDiff <= hydrationWindowMin)
                {
                    var otherProduct = products.FirstOrDefault(p => p.Name == otherEvt.ProductName);
                    if (otherProduct != null && 
                        otherProduct.Texture == ProductTexture.Drink && 
                        otherProduct.VolumeMl >= 100)
                    {
                        hasNearbyHydration = true;
                        break;
                    }
                }
            }

            if (!hasNearbyHydration)
            {
                warnings.Add($"Gel at {evt.TimeMin}min ({evt.ProductName}) may need additional hydration. " +
                           $"Consider pairing with water or sports drink within {hydrationWindowMin} minutes.");
            }
        }

        return warnings;
    }

    #endregion
}
