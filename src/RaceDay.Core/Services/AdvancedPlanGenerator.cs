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
    private sealed record PhaseSegment(RacePhase Phase, double StartHour, double EndHour);
    private sealed record Slot(int TimeMin, RacePhase Phase);

    /// <summary>
    /// Planner state tracking during generation
    /// </summary>
    private sealed class PlannerState
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
        var context = InitializePlanningContext(race, athlete);
        var plan = new List<NutritionEvent>();

        AddPreRaceEvent(context, products, plan);
        AddMainRaceEvents(context, products, plan);
        AddBackupEventIfNeeded(context, products, plan);

        return plan;
    }

    /// <summary>
    /// Initialize planning context with race configuration and state
    /// </summary>
    private PlanningContext InitializePlanningContext(RaceProfile race, AthleteProfile athlete)
    {
        var raceMode = DetermineRaceMode(race.SportType, race.DurationHours);
        var slotInterval = GetSlotInterval(raceMode);
        var durationHours = race.DurationHours;
        var durationMinutes = (int)(durationHours * 60);
        var weightKg = athlete.WeightKg;

        var phases = BuildPhaseTimeline(raceMode, durationHours);
        var slots = BuildSlots(durationMinutes, slotInterval, phases);
        var carbsPerHour = CalculateCarbsPerHour(raceMode, weightKg);
        var totalCarbs = carbsPerHour * durationHours;
        var state = InitPlannerState(raceMode, weightKg);
        // Use fixed seed for reproducible results
        var random = new Random(42);

        return new PlanningContext(
            raceMode, durationHours, durationMinutes, weightKg,
            phases, slots, totalCarbs, state, random);
    }

    /// <summary>
    /// Add pre-race nutrition event (15 minutes before start)
    /// </summary>
    private void AddPreRaceEvent(
        PlanningContext context,
        List<ProductEnhanced> products,
        List<NutritionEvent> plan)
    {
        var preRaceProduct = products.FirstOrDefault(p => p.Texture == ProductTexture.Bake);
        if (preRaceProduct == null)
            return;

        var mainPhase = context.Phases.FirstOrDefault()?.Phase ?? RacePhase.Run;
        context.State.TotalCarbs += preRaceProduct.CarbsG;

        var nutritionEvent = CreateNutritionEvent(
            timeMin: -15,
            phase: mainPhase,
            product: preRaceProduct,
            action: "Eat",
            totalCarbsSoFar: context.State.TotalCarbs);

        plan.Add(nutritionEvent);
    }

    /// <summary>
    /// Add main race nutrition events across all slots
    /// </summary>
    private void AddMainRaceEvents(
        PlanningContext context,
        List<ProductEnhanced> products,
        List<NutritionEvent> plan)
    {
        foreach (var slot in context.Slots)
        {
            // Skip eating during swim
            if (slot.Phase == RacePhase.Swim)
                continue;

            var product = TrySelectAndValidateProduct(context, slot, products);
            if (product == null)
                continue;

            UpdateStateWithProduct(context, slot.TimeMin, product);

            var nutritionEvent = CreateNutritionEvent(
                timeMin: slot.TimeMin,
                phase: slot.Phase,
                product: product,
                action: GetAction(product.Texture),
                totalCarbsSoFar: context.State.TotalCarbs);

            plan.Add(nutritionEvent);
        }
    }

    /// <summary>
    /// Add backup nutrition event if carb target not met
    /// </summary>
    private void AddBackupEventIfNeeded(
        PlanningContext context,
        List<ProductEnhanced> products,
        List<NutritionEvent> plan)
    {
        if (context.State.TotalCarbs >= context.TotalCarbs * 0.9)
            return;

        var extraProduct = SelectExtraProduct(context.RaceMode, products);
        if (extraProduct == null)
            return;

        context.State.TotalCarbs += extraProduct.CarbsG;
        var finalPhase = context.RaceMode == RaceMode.Cycling ? RacePhase.Bike : RacePhase.Run;

        var nutritionEvent = CreateNutritionEvent(
            timeMin: context.DurationMinutes,
            phase: finalPhase,
            product: extraProduct,
            action: GetAction(extraProduct.Texture),
            totalCarbsSoFar: context.State.TotalCarbs);

        plan.Add(nutritionEvent);
    }

    /// <summary>
    /// Select and validate product for a slot, handling caffeine limits
    /// </summary>
    private ProductEnhanced? TrySelectAndValidateProduct(
        PlanningContext context,
        Slot slot,
        List<ProductEnhanced> products)
    {
        double currentHour = slot.TimeMin / 60.0;
        bool isEndPhase = currentHour > context.DurationHours * AdvancedNutritionConfig.EndPhaseThreshold;
        bool wantsCaffeine = ShouldUseCaffeine(currentHour, context.WeightKg, context.State);

        var product = SelectProductForSlot(
            context.RaceMode,
            isEndPhase,
            wantsCaffeine,
            products,
            context.Random);

        if (product == null)
            return null;

        // Validate and adjust for caffeine limits
        if (wantsCaffeine && product.HasCaffeine)
        {
            double maxCaffeine = AdvancedNutritionConfig.MaxCaffeineMgPerKg * context.WeightKg;
            if (context.State.TotalCaffeineMg + product.CaffeineMg > maxCaffeine)
            {
                // Fall back to non-caffeine version
                product = products.FirstOrDefault(p =>
                    !p.HasCaffeine &&
                    p.Texture == product.Texture &&
                    p.ProductType == product.ProductType) ?? product;
            }
        }

        return product;
    }

    /// <summary>
    /// Update planner state after consuming a product
    /// </summary>
    private void UpdateStateWithProduct(PlanningContext context, int timeMin, ProductEnhanced product)
    {
        context.State.TotalCarbs += product.CarbsG;

        if (product.HasCaffeine)
        {
            double currentHour = timeMin / 60.0;
            context.State.TotalCaffeineMg += product.CaffeineMg;
            context.State.NextCaffeineHour = currentHour + AdvancedNutritionConfig.CaffeineIntervalHours;
        }
    }

    /// <summary>
    /// Create a nutrition event with consistent structure
    /// </summary>
    private NutritionEvent CreateNutritionEvent(
        int timeMin,
        RacePhase phase,
        ProductEnhanced product,
        string action,
        double totalCarbsSoFar)
    {
        return new NutritionEvent(
            TimeMin: timeMin,
            Phase: phase,
            PhaseDescription: GetPhaseDescription(phase),
            ProductName: product.Name,
            AmountPortions: 1,
            Action: action,
            TotalCarbsSoFar: totalCarbsSoFar,
            HasCaffeine: product.HasCaffeine,
            CaffeineMg: product.HasCaffeine ? product.CaffeineMg : null);
    }

    /// <summary>
    /// Context object holding all planning state and configuration
    /// </summary>
    private sealed record PlanningContext(
        RaceMode RaceMode,
        double DurationHours,
        int DurationMinutes,
        double WeightKg,
        List<PhaseSegment> Phases,
        List<Slot> Slots,
        double TotalCarbs,
        PlannerState State,
        Random Random);

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

    private static RaceMode DetermineRaceMode(SportType sportType, double durationHours) =>
        sportType switch
        {
            SportType.Bike => RaceMode.Cycling,
            SportType.Triathlon when durationHours >= 6 => RaceMode.TriathlonFull,
            SportType.Triathlon => RaceMode.TriathlonHalf,
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
        return mode switch
        {
            RaceMode.TriathlonHalf => new List<PhaseSegment>
            {
                // Half triathlon: ~10% swim, 50% bike, 40% run
                new(RacePhase.Swim, 0, totalHours * 0.10),
                new(RacePhase.Bike, totalHours * 0.10, totalHours * 0.60),
                new(RacePhase.Run, totalHours * 0.60, totalHours)
            },
            RaceMode.TriathlonFull => new List<PhaseSegment>
            {
                // Full triathlon: ~12% swim, 55% bike, 33% run
                new(RacePhase.Swim, 0, totalHours * 0.12),
                new(RacePhase.Bike, totalHours * 0.12, totalHours * 0.67),
                new(RacePhase.Run, totalHours * 0.67, totalHours)
            },
            RaceMode.Cycling => new List<PhaseSegment> { new(RacePhase.Bike, 0, totalHours) },
            _ => new List<PhaseSegment> { new(RacePhase.Run, 0, totalHours) }
        };
    }

    private static List<Slot> BuildSlots(int durationMinutes, int slotInterval, List<PhaseSegment> phases)
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
        {
            // Prefer light gels, fall back to regular gels if not available
            var lightGels = products.Where(p => p.Texture == ProductTexture.LightGel).ToList();
            if (lightGels.Any())
                return lightGels;
            return products.Where(p => p.Texture == ProductTexture.Gel);
        }

        if (isEndPhase)
            return products.Where(p => p.Texture == ProductTexture.Gel || p.Texture == ProductTexture.LightGel);

        return products.Where(p => p.Texture == ProductTexture.Gel || p.Texture == ProductTexture.LightGel);
    }

    private static ProductEnhanced? SelectExtraProduct(RaceMode mode, List<ProductEnhanced> products) =>
        mode switch
        {
            RaceMode.Cycling => products.FirstOrDefault(p => p.Texture == ProductTexture.Chew || p.Texture == ProductTexture.Bake),
            _ => products.FirstOrDefault(p => p.Texture == ProductTexture.Gel)
        };

    private static string GetAction(ProductTexture texture) =>
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
