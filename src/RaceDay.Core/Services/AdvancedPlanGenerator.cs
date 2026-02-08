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
    private const int ReproducibleRandomSeed = 42;
    private const double CarbTargetThreshold = 0.9;
    private const int PreRaceMinutes = -15;

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
    /// <summary>
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
        var state = InitPlannerState(raceMode);
        // S2245: Using seeded Random for reproducible test results across runs
        var random = new Random(ReproducibleRandomSeed);

        return new PlanningContext(
            raceMode, durationHours, durationMinutes, weightKg,
            phases, slots, totalCarbs, state, random);
    }

    /// Add pre-race nutrition event (15 minutes before start)
    /// </summary>
    private static void AddPreRaceEvent(
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
            timeMin: PreRaceMinutes,
            phase: mainPhase,
            product: preRaceProduct,
            action: "Eat",
            totalCarbsSoFar: context.State.TotalCarbs);

        plan.Add(nutritionEvent);
    }

    /// <summary>
    /// Add main race nutrition events across all slots
    /// </summary>
    private static void AddMainRaceEvents(
        PlanningContext context,
        List<ProductEnhanced> products,
        List<NutritionEvent> plan)
    {
        foreach (var slot in context.Slots)
        {
            if (IsSwimPhase(slot))
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
    private static void AddBackupEventIfNeeded(
        PlanningContext context,
        List<ProductEnhanced> products,
        List<NutritionEvent> plan)
    {
        if (HasMetCarbTarget(context))
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
    private static ProductEnhanced? TrySelectAndValidateProduct(
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

        if (wantsCaffeine && product.HasCaffeine && WouldExceedCaffeineLimit(context, product))
        {
            product = FindNonCaffeinatedAlternative(products, product) ?? product;
        }

        return product;
    }

    /// <summary>
    /// Update planner state after consuming a product
    /// </summary>
    private static void UpdateStateWithProduct(PlanningContext context, int timeMin, ProductEnhanced product)
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
    private static NutritionEvent CreateNutritionEvent(
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

    private static bool IsSwimPhase(Slot slot) => slot.Phase == RacePhase.Swim;

    private static bool HasMetCarbTarget(PlanningContext context) =>
        context.State.TotalCarbs >= context.TotalCarbs * CarbTargetThreshold;

    private static bool WouldExceedCaffeineLimit(PlanningContext context, ProductEnhanced product)
    {
        double maxCaffeine = AdvancedNutritionConfig.MaxCaffeineMgPerKg * context.WeightKg;
        return context.State.TotalCaffeineMg + product.CaffeineMg > maxCaffeine;
    }

    private static ProductEnhanced? FindNonCaffeinatedAlternative(
        List<ProductEnhanced> products,
        ProductEnhanced caffeinated) =>
        products.FirstOrDefault(p =>
            !p.HasCaffeine &&
            p.Texture == caffeinated.Texture &&
            p.ProductType == caffeinated.ProductType);

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

    private static class TriathlonPhaseDistribution
    {
        public const double HalfSwimEnd = 0.10;
        public const double HalfBikeEnd = 0.60;
        public const double FullSwimEnd = 0.12;
        public const double FullBikeEnd = 0.67;
    }

    private static List<PhaseSegment> BuildPhaseTimeline(RaceMode mode, double totalHours)
    {
        return mode switch
        {
            RaceMode.TriathlonHalf => new List<PhaseSegment>
            {
                new(RacePhase.Swim, 0, totalHours * TriathlonPhaseDistribution.HalfSwimEnd),
                new(RacePhase.Bike, totalHours * TriathlonPhaseDistribution.HalfSwimEnd, totalHours * TriathlonPhaseDistribution.HalfBikeEnd),
                new(RacePhase.Run, totalHours * TriathlonPhaseDistribution.HalfBikeEnd, totalHours)
            },
            RaceMode.TriathlonFull => new List<PhaseSegment>
            {
                new(RacePhase.Swim, 0, totalHours * TriathlonPhaseDistribution.FullSwimEnd),
                new(RacePhase.Bike, totalHours * TriathlonPhaseDistribution.FullSwimEnd, totalHours * TriathlonPhaseDistribution.FullBikeEnd),
                new(RacePhase.Run, totalHours * TriathlonPhaseDistribution.FullBikeEnd, totalHours)
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

    private PlannerState InitPlannerState(RaceMode mode)
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
            return SelectLightGelsOrFallbackToRegular(products);

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

    private static IEnumerable<ProductEnhanced> SelectLightGelsOrFallbackToRegular(List<ProductEnhanced> products)
    {
        var lightGels = products.Where(p => p.Texture == ProductTexture.LightGel).ToList();
        return lightGels.Any() ? lightGels : products.Where(p => p.Texture == ProductTexture.Gel);
    }

    #endregion
}
