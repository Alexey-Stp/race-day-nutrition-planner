namespace RaceDay.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using RaceDay.Core.Models;
using RaceDay.Core.Constants;

/// <summary>
/// Advanced nutrition planner with sport-specific logic, caffeine strategy, and phase awareness
/// </summary>
public class AdvancedPlanGenerator
{
    private record PhaseSegment(RacePhase Phase, double StartHour, double EndHour);
    private record Slot(int TimeMin, RacePhase Phase);

    /// <summary>
    /// Planner state tracking during generation
    /// </summary>
    private class PlannerState
    {
        public double TotalCarbs { get; set; }
        public double TotalCaffeineMg { get; set; }
        public double NextCaffeineHour { get; set; }
    }

    /// <summary>
    /// Generate an advanced nutrition plan based on race characteristics
    /// </summary>
    public List<NutritionEvent> GeneratePlan(
        RaceProfile race,
        AthleteProfile athlete,
        List<ProductEnhanced> products,
        int intervalMinutes = 22)
    {
        var raceMode = DetermineRaceMode(race.SportType);
        var slotInterval = GetSlotInterval(raceMode);
        var durationHours = race.DurationHours;
        var durationMinutes = (int)(durationHours * 60);
        var weightKg = athlete.WeightKg;

        // Build phase timeline (important for triathlon)
        var phases = BuildPhaseTimeline(raceMode, durationHours);
        var slots = BuildSlots(durationMinutes, slotInterval, phases);

        // Calculate targets
        var carbsPerHour = CalculateCarbsPerHour(raceMode, weightKg);
        var totalCarbs = carbsPerHour * durationHours;

        // Initialize planner state
        var state = InitPlannerState(raceMode, weightKg);
        var random = new Random(42); // Deterministic for testing

        var plan = new List<NutritionEvent>();

        // Pre-race intake (15 min before)
        var preRaceProduct = products.FirstOrDefault(p => p.Texture == ProductTexture.Bake);
        if (preRaceProduct != null)
        {
            plan.Add(new NutritionEvent(
                TimeMin: -15,
                Phase: RacePhase.Swim, // Generic phase for pre-race
                ProductName: preRaceProduct.Name,
                AmountPortions: 1,
                Action: "Eat",
                TotalCarbsSoFar: state.TotalCarbs += preRaceProduct.CarbsG,
                HasCaffeine: false
            ));
        }

        // Run phase detection for better product selection
        var runSegment = phases.FirstOrDefault(p => p.Phase == RacePhase.Run);
        var bikeSegment = phases.FirstOrDefault(p => p.Phase == RacePhase.Bike);
        double runStartHour = runSegment?.StartHour ?? 0;
        double bikeStartHour = bikeSegment?.StartHour ?? 0;

        // Main race schedule
        foreach (var slot in slots)
        {
            // Skip eating during swim
            if (slot.Phase == RacePhase.Swim)
                continue;

            double currentHour = slot.TimeMin / 60.0;
            bool isEndPhase = currentHour > durationHours * AdvancedNutritionConfig.EndPhaseThreshold;
            bool wantsCaffeine = ShouldUseCaffeine(currentHour, weightKg, state);

            var product = SelectProductForSlot(
                raceMode,
                slot,
                isEndPhase,
                wantsCaffeine,
                products,
                random,
                runStartHour,
                bikeStartHour);

            if (product == null)
                continue;

            // Validate caffeine doesn't exceed limit
            if (wantsCaffeine && product.HasCaffeine)
            {
                double maxCaffeine = AdvancedNutritionConfig.MaxCaffeineMgPerKg * weightKg;
                if (state.TotalCaffeineMg + product.CaffeineMg > maxCaffeine)
                {
                    // Fall back to non-caffeine version
                    product = products.FirstOrDefault(p =>
                        !p.HasCaffeine &&
                        p.Texture == product.Texture &&
                        p.ProductType == product.ProductType) ?? product;
                }
            }

            // Update state
            state.TotalCarbs += product.CarbsG;
            if (product.HasCaffeine)
            {
                state.TotalCaffeineMg += product.CaffeineMg;
                state.NextCaffeineHour = currentHour + AdvancedNutritionConfig.CaffeineIntervalHours;
            }

            plan.Add(new NutritionEvent(
                TimeMin: slot.TimeMin,
                Phase: slot.Phase,
                ProductName: product.Name,
                AmountPortions: 1,
                Action: GetAction(product.Texture),
                TotalCarbsSoFar: state.TotalCarbs,
                HasCaffeine: product.HasCaffeine
            ));
        }

        // Add extra product if under target
        if (state.TotalCarbs < totalCarbs * 0.9)
        {
            var extraProduct = SelectExtraProduct(raceMode, products);
            if (extraProduct != null)
            {
                state.TotalCarbs += extraProduct.CarbsG;
                plan.Add(new NutritionEvent(
                    TimeMin: durationMinutes,
                    Phase: raceMode == RaceMode.Cycling ? RacePhase.Bike : RacePhase.Run,
                    ProductName: extraProduct.Name,
                    AmountPortions: 1,
                    Action: GetAction(extraProduct.Texture),
                    TotalCarbsSoFar: state.TotalCarbs,
                    HasCaffeine: extraProduct.HasCaffeine
                ));
            }
        }

        return plan;
    }

    #region Helper Methods

    private enum RaceMode { Running, Cycling, TriathlonHalf, TriathlonFull }

    private RaceMode DetermineRaceMode(SportType sportType) =>
        sportType switch
        {
            SportType.Bike => RaceMode.Cycling,
            _ => RaceMode.Running // Triathlon support would come from duration/context
        };

    private int GetSlotInterval(RaceMode mode) =>
        mode switch
        {
            RaceMode.TriathlonHalf or RaceMode.TriathlonFull => AdvancedNutritionConfig.TriathlonSlotIntervalMin,
            RaceMode.Cycling => AdvancedNutritionConfig.CyclingSlotIntervalMin,
            _ => AdvancedNutritionConfig.RunningSlotIntervalMin
        };

    private double CalculateCarbsPerHour(RaceMode mode, double weightKg)
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

    private List<PhaseSegment> BuildPhaseTimeline(RaceMode mode, double totalHours)
    {
        // Currently supporting simple modes; triathlon logic can be added
        var phase = mode == RaceMode.Cycling ? RacePhase.Bike : RacePhase.Run;
        return new List<PhaseSegment> { new(phase, 0, totalHours) };
    }

    private List<Slot> BuildSlots(int durationMinutes, int slotInterval, List<PhaseSegment> phases)
    {
        var slots = new List<Slot>();
        int numSlots = (int)Math.Ceiling((double)durationMinutes / slotInterval);

        for (int i = 1; i <= numSlots; i++)
        {
            int timeMin = i * slotInterval;
            if (timeMin > durationMinutes)
                timeMin = durationMinutes;

            double hour = timeMin / 60.0;
            var phase = phases
                .LastOrDefault(p => hour >= p.StartHour && hour <= p.EndHour)
                ?.Phase ?? RacePhase.Run;

            slots.Add(new Slot(timeMin, phase));
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

    private bool ShouldUseCaffeine(double currentHour, double weightKg, PlannerState state)
    {
        double maxTotal = AdvancedNutritionConfig.MaxCaffeineMgPerKg * weightKg;

        if (currentHour < state.NextCaffeineHour)
            return false;

        if (state.TotalCaffeineMg >= maxTotal)
            return false;

        return currentHour >= state.NextCaffeineHour - 0.1;
    }

    private ProductEnhanced? SelectProductForSlot(
        RaceMode mode,
        Slot slot,
        bool isEndPhase,
        bool wantsCaffeine,
        List<ProductEnhanced> products,
        Random random,
        double runStartHour,
        double bikeStartHour)
    {
        var candidates = mode switch
        {
            RaceMode.Cycling => SelectCyclingCandidates(isEndPhase, products, random),
            _ => SelectRunningCandidates(isEndPhase, products, random)
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

    private IEnumerable<ProductEnhanced> SelectCyclingCandidates(
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

    private IEnumerable<ProductEnhanced> SelectRunningCandidates(
        bool isEndPhase,
        List<ProductEnhanced> products,
        Random random)
    {
        if (!isEndPhase && random.NextDouble() < 0.3)
            return products.Where(p => p.Texture == ProductTexture.LightGel);

        if (isEndPhase)
            return products.Where(p => p.Texture == ProductTexture.Gel);

        return products.Where(p => p.Texture == ProductTexture.Gel || p.Texture == ProductTexture.LightGel);
    }

    private ProductEnhanced? SelectExtraProduct(RaceMode mode, List<ProductEnhanced> products) =>
        mode switch
        {
            RaceMode.Cycling => products.FirstOrDefault(p => p.Texture == ProductTexture.Chew || p.Texture == ProductTexture.Bake),
            _ => products.FirstOrDefault(p => p.Texture == ProductTexture.Gel)
        };

    private string GetAction(ProductTexture texture) =>
        texture switch
        {
            ProductTexture.Gel or ProductTexture.LightGel => "Squeeze",
            ProductTexture.Drink => "Drink",
            ProductTexture.Chew => "Chew",
            ProductTexture.Bake => "Eat",
            _ => "Consume"
        };

    #endregion
}
