namespace RaceDay.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using RaceDay.Core.Models;
using RaceDay.Core.Constants;

/// <summary>
/// Advanced nutrition planner with sip-based drink scheduling, full-duration distribution,
/// target reconciliation, and sport-specific logic.
/// </summary>
public class PlanGenerator
{
    private sealed record PhaseSegment(RacePhase Phase, double StartHour, double EndHour);
    private sealed record Slot(int TimeMin, RacePhase Phase);

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

    // ─── Public API ──────────────────────────────────────────────────

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

        var phases = BuildPhaseTimeline(race.SportType, durationHours);
        var slots = BuildSlots(durationMinutes, slotInterval, phases);
        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled);
        var totalCarbs = targets.CarbsG;

        var state = InitPlannerState(raceMode, weightKg);
        var plan = new List<NutritionEvent>();
        var mainPhase = phases.FirstOrDefault()?.Phase ?? RacePhase.Run;

        // === PHASE 1: Pre-race intake ===
        AddPreRaceIntake(plan, state, products, mainPhase, targets);

        // === PHASE 2: Build sip-based drink backbone ===
        BuildSipDrinkBackbone(plan, state, products, phases, targets, durationMinutes);

        // === PHASE 3: Fill remaining slots with scored selection (gels, bars, chews) ===
        FillSlotsWithScoredProducts(plan, state, products, slots, targets, durationHours, caffeineEnabled);

        // === PHASE 4: Fill gaps if under target ===
        FillGapsIfUnderTarget(plan, state, products, phases, targets, durationMinutes, raceMode);

        // === PHASE 5: Recalculate cumulative totals ===
        RecalculateCumulativeTotals(plan, products);

        // === PHASE 6: Target reconciliation ===
        ReconcileToTarget(plan, products, targets, durationMinutes, phases);

        // === PHASE 7: Validate ===
        var validationResult = ValidateAndAutoFix(plan, targets, products, durationMinutes, caffeineEnabled);
        return validationResult.Plan;
    }

    public PlanResult GeneratePlanWithDiagnostics(
        RaceProfile race,
        AthleteProfile athlete,
        List<ProductEnhanced> products,
        int intervalMinutes = 22,
        bool caffeineEnabled = false)
    {
        var plan = GeneratePlan(race, athlete, products, intervalMinutes, caffeineEnabled);
        var targets = NutritionCalculator.CalculateMultiNutrientTargets(race, athlete, caffeineEnabled);
        var durationMinutes = (int)(race.DurationHours * 60);
        var validationResult = ValidateAndAutoFix(plan, targets, products, durationMinutes, caffeineEnabled);

        return new PlanResult(
            validationResult.Plan,
            validationResult.Warnings,
            validationResult.Errors
        );
    }

    // ─── Phase 1: Pre-race intake ─────────────────────────────────

    private static void AddPreRaceIntake(
        List<NutritionEvent> plan,
        PlannerState state,
        List<ProductEnhanced> products,
        RacePhase mainPhase,
        MultiNutrientTargets targets)
    {
        if (state.TotalCarbs >= targets.CarbsG * 0.1)
            return;

        var preRaceCandidates = products.Where(p => !p.HasCaffeine).ToList();
        var preRaceProduct = preRaceCandidates.FirstOrDefault(p => p.Texture == ProductTexture.Bake)
                          ?? preRaceCandidates.FirstOrDefault(p => p.Texture == ProductTexture.Gel);
        if (preRaceProduct == null)
            return;

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
            HasCaffeine: false,
            CaffeineMg: null,
            TotalCaffeineSoFar: 0,
            CarbsInEvent: preRaceProduct.CarbsG
        ));
    }

    // ─── Phase 2: Sip-based drink backbone ────────────────────────

    private static void BuildSipDrinkBackbone(
        List<NutritionEvent> plan,
        PlannerState state,
        List<ProductEnhanced> products,
        List<PhaseSegment> phases,
        MultiNutrientTargets targets,
        int durationMinutes)
    {
        var highCarbDrinks = products
            .Where(p => p.Texture == ProductTexture.Drink && p.CarbsG > 20)
            .OrderByDescending(p => p.CarbsG)
            .ToList();

        if (!highCarbDrinks.Any())
            return;

        // Target ~40-50% of carbs from sipped drinks
        double drinkTargetCarbs = targets.CarbsG * 0.45;
        var drink = highCarbDrinks.First();

        // Determine drink volume and concentration
        double drinkVolume = drink.VolumeMl > 0 ? drink.VolumeMl : SchedulingConstraints.DefaultDrinkVolumeMl;
        double carbsPerMl = drink.CarbsG / drinkVolume;
        double sipMl = SchedulingConstraints.SipVolumeMl;
        double carbsPerSip = carbsPerMl * sipMl;
        int sipInterval = SchedulingConstraints.SipIntervalMinutes;

        // Determine which phases to place sips in
        var bikePhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Bike);
        bool isTriathlon = bikePhase != null && phases.Any(p => p.Phase == RacePhase.Run);

        if (isTriathlon)
        {
            // Triathlon: sip on bike, lighter sipping on run
            int bikeStartMin = (int)(bikePhase!.StartHour * 60);
            int bikeEndMin = (int)(bikePhase.EndHour * 60) - AdvancedNutritionConfig.BikeToRunTransitionMarginMin;
            double bikeDrinkTarget = drinkTargetCarbs * 0.75;

            ScheduleSips(plan, state, drink, bikeStartMin + 5, bikeEndMin, sipInterval,
                         sipMl, carbsPerSip, bikeDrinkTarget, RacePhase.Bike);

            // Run phase sipping
            var runPhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Run);
            if (runPhase != null && state.TotalCarbs < drinkTargetCarbs)
            {
                int runStartMin = (int)(runPhase.StartHour * 60);
                int runEndMin = Math.Min((int)(runPhase.EndHour * 60) - 5, durationMinutes - 5);
                ScheduleSips(plan, state, drink, runStartMin + 5, runEndMin, sipInterval + 5,
                             sipMl, carbsPerSip, drinkTargetCarbs, RacePhase.Run);
            }
        }
        else
        {
            // Non-triathlon: spread sips across entire race
            var racePhase = phases.FirstOrDefault()?.Phase ?? RacePhase.Run;
            int startMin = 10;
            int endMin = durationMinutes - 5;
            ScheduleSips(plan, state, drink, startMin, endMin, sipInterval,
                         sipMl, carbsPerSip, drinkTargetCarbs, racePhase);
        }
    }

    private static void ScheduleSips(
        List<NutritionEvent> plan,
        PlannerState state,
        ProductEnhanced drink,
        int startMin, int endMin, int interval,
        double sipMl, double carbsPerSip, double carbsLimit,
        RacePhase phase)
    {
        double drinkVolume = drink.VolumeMl > 0 ? drink.VolumeMl : SchedulingConstraints.DefaultDrinkVolumeMl;
        double carbsPerMl = drink.CarbsG / drinkVolume;

        for (int timeMin = startMin; timeMin <= endMin && state.TotalCarbs < carbsLimit; timeMin += interval)
        {
            // Clamp sip to not exceed limit
            double remainingCarbs = carbsLimit - state.TotalCarbs;
            double actualCarbsPerSip = Math.Min(carbsPerSip, remainingCarbs);
            double actualSipMl = actualCarbsPerSip / carbsPerMl;

            if (actualSipMl < SchedulingConstraints.MinSipVolumeMl)
                break;

            double portionFraction = actualSipMl / drinkVolume;

            state.TotalCarbs += actualCarbsPerSip;
            state.TotalSodium += drink.SodiumMg * portionFraction;
            state.TotalFluid += actualSipMl;
            if (drink.HasCaffeine)
                state.TotalCaffeineMg += drink.CaffeineMg * portionFraction;
            state.RecordProduct(drink.Name);

            plan.Add(new NutritionEvent(
                TimeMin: timeMin,
                Phase: phase,
                PhaseDescription: GetPhaseDescription(phase),
                ProductName: drink.Name,
                AmountPortions: Math.Round(portionFraction, 2),
                Action: "Sip",
                TotalCarbsSoFar: state.TotalCarbs,
                HasCaffeine: drink.HasCaffeine,
                CaffeineMg: drink.HasCaffeine ? Math.Round(drink.CaffeineMg * portionFraction, 1) : null,
                TotalCaffeineSoFar: state.TotalCaffeineMg,
                CarbsInEvent: Math.Round(actualCarbsPerSip, 1),
                SipMl: Math.Round(actualSipMl, 0)
            ));
        }
    }

    // ─── Phase 3: Fill slots with scored products ─────────────────

    private static void FillSlotsWithScoredProducts(
        List<NutritionEvent> plan,
        PlannerState state,
        List<ProductEnhanced> products,
        List<Slot> slots,
        MultiNutrientTargets targets,
        double durationHours,
        bool caffeineEnabled)
    {
        foreach (var slot in slots)
        {
            if (slot.Phase == RacePhase.Swim)
                continue;

            // Skip if a non-sip event already exists within the cluster window
            bool slotOccupied = plan.Any(e =>
                e.Action != "Sip" &&
                Math.Abs(e.TimeMin - slot.TimeMin) < SchedulingConstraints.ClusterWindow);
            if (slotOccupied)
                continue;

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

            if (remainingNeeds.CarbsG < 5)
                continue; // Skip this slot but keep iterating to later slots

            // Select non-drink product (drinks handled by sip backbone)
            var nonDrinkProducts = products.Where(p => p.Texture != ProductTexture.Drink).ToList();
            var product = SelectBestProduct(nonDrinkProducts, slot.Phase, remainingNeeds, state, raceProgress, caffeineEnabled);
            if (product == null)
            {
                // Fall back to any product if no non-drink available
                product = SelectBestProduct(products, slot.Phase, remainingNeeds, state, raceProgress, caffeineEnabled);
            }
            if (product == null)
                continue;

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
                TotalCaffeineSoFar: state.TotalCaffeineMg,
                CarbsInEvent: product.CarbsG
            ));
        }
    }

    // ─── Phase 4: Fill gaps if under target ───────────────────────

    private static void FillGapsIfUnderTarget(
        List<NutritionEvent> plan,
        PlannerState state,
        List<ProductEnhanced> products,
        List<PhaseSegment> phases,
        MultiNutrientTargets targets,
        int durationMinutes,
        RaceMode raceMode)
    {
        int safetyMarginMinutes = 5;
        int maxExtraTimeMin = durationMinutes - safetyMarginMinutes;
        bool isTriathlon = phases.Any(p => p.Phase == RacePhase.Bike) && phases.Any(p => p.Phase == RacePhase.Run);
        var finalPhase = phases.LastOrDefault()?.Phase ?? (raceMode == RaceMode.Cycling ? RacePhase.Bike : RacePhase.Run);

        if (state.TotalCarbs >= targets.CarbsG)
            return;

        if (isTriathlon)
        {
            var bikePhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Bike);
            var runPhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Run);
            if (bikePhase == null || runPhase == null) return;

            double remainingCarbs = targets.CarbsG - state.TotalCarbs;
            double bikeTargetCarbs = state.TotalCarbs + (remainingCarbs * AdvancedNutritionConfig.TriathlonBikeCarbsRatio);
            int bikeEndMin = (int)(bikePhase.EndHour * 60) - AdvancedNutritionConfig.BikeToRunTransitionMarginMin;
            int bikeStartMin = (int)(bikePhase.StartHour * 60);

            AddExtraProducts(plan, state, products, raceMode, bikeStartMin, bikeEndMin, RacePhase.Bike, bikeTargetCarbs, targets.CarbsG, 15);

            int runStartMin = (int)(runPhase.StartHour * 60);
            int runEndMin = Math.Min((int)(runPhase.EndHour * 60), maxExtraTimeMin);
            AddExtraProducts(plan, state, products, raceMode, runStartMin, runEndMin, RacePhase.Run, targets.CarbsG, targets.CarbsG, 20);
        }
        else
        {
            // Distribute across full duration, not just from end
            int startMin = 10;
            AddExtraProducts(plan, state, products, raceMode, startMin, maxExtraTimeMin, finalPhase, targets.CarbsG, targets.CarbsG, 15);
        }
    }

    private static void AddExtraProducts(
        List<NutritionEvent> plan,
        PlannerState state,
        List<ProductEnhanced> products,
        RaceMode raceMode,
        int startMin, int endMin,
        RacePhase phase,
        double phaseTargetCarbs,
        double totalTargetCarbs,
        int minSpacing)
    {
        int attempts = 0;
        while (state.TotalCarbs < phaseTargetCarbs && state.TotalCarbs < totalTargetCarbs && attempts < 30)
        {
            var extraProduct = SelectExtraProduct(raceMode, products);
            if (extraProduct == null) break;

            var existingTimes = plan.Where(e => e.Phase == phase && e.Action != "Sip")
                .Select(e => e.TimeMin).OrderBy(t => t).ToList();

            int? timeSlot = FindAvailableTimeSlot(startMin, endMin, existingTimes, minSpacing);
            if (!timeSlot.HasValue) break;

            state.TotalCarbs += extraProduct.CarbsG;
            if (extraProduct.HasCaffeine)
                state.TotalCaffeineMg += extraProduct.CaffeineMg;

            plan.Add(new NutritionEvent(
                TimeMin: timeSlot.Value,
                Phase: phase,
                PhaseDescription: GetPhaseDescription(phase),
                ProductName: extraProduct.Name,
                AmountPortions: 1,
                Action: GetAction(extraProduct.Texture),
                TotalCarbsSoFar: state.TotalCarbs,
                HasCaffeine: extraProduct.HasCaffeine,
                CaffeineMg: extraProduct.HasCaffeine ? extraProduct.CaffeineMg : null,
                TotalCaffeineSoFar: state.TotalCaffeineMg,
                CarbsInEvent: extraProduct.CarbsG
            ));
            attempts++;
        }
    }

    // ─── Phase 5: Recalculate cumulative totals ───────────────────

    private static void RecalculateCumulativeTotals(List<NutritionEvent> plan, List<ProductEnhanced> products)
    {
        plan.Sort((a, b) => a.TimeMin.CompareTo(b.TimeMin));
        double cumulativeCarbs = 0;
        double cumulativeCaffeine = 0;

        for (int i = 0; i < plan.Count; i++)
        {
            var evt = plan[i];
            // Use CarbsInEvent if set, otherwise fall back to product lookup
            double eventCarbs = evt.CarbsInEvent > 0
                ? evt.CarbsInEvent
                : (products.FirstOrDefault(p => p.Name == evt.ProductName)?.CarbsG ?? 0);

            double eventCaffeine = 0;
            if (evt.HasCaffeine && evt.CaffeineMg.HasValue)
                eventCaffeine = evt.CaffeineMg.Value;

            cumulativeCarbs += eventCarbs;
            cumulativeCaffeine += eventCaffeine;

            plan[i] = evt with
            {
                TotalCarbsSoFar = Math.Round(cumulativeCarbs, 1),
                TotalCaffeineSoFar = Math.Round(cumulativeCaffeine, 1),
                CarbsInEvent = Math.Round(eventCarbs, 1)
            };
        }
    }

    // ─── Phase 6: Target reconciliation ───────────────────────────

    private static void ReconcileToTarget(
        List<NutritionEvent> plan,
        List<ProductEnhanced> products,
        MultiNutrientTargets targets,
        int durationMinutes,
        List<PhaseSegment> phases)
    {
        double actualTotal = plan.Sum(e => e.CarbsInEvent);
        double targetCarbs = targets.CarbsG;
        double tolerance = targetCarbs * SchedulingConstraints.TargetTolerancePercent;

        // Overshoot: trim sip events first, then gel/bar events from end
        // Only remove if doing so won't push us below target
        if (actualTotal > targetCarbs + tolerance)
        {
            // Remove sip events from end first, but stay at or above target
            var sipEvents = plan.Where(e => e.Action == "Sip").OrderByDescending(e => e.TimeMin).ToList();
            foreach (var sipEvent in sipEvents)
            {
                if (actualTotal <= targetCarbs + tolerance) break;
                double afterRemoval = actualTotal - sipEvent.CarbsInEvent;
                if (afterRemoval < targetCarbs) continue; // Don't cause undershoot
                actualTotal -= sipEvent.CarbsInEvent;
                plan.Remove(sipEvent);
            }

            // If still significantly over, remove non-sip events from end
            if (actualTotal > targetCarbs + tolerance)
            {
                var nonSipEvents = plan.Where(e => e.Action != "Sip" && e.TimeMin > 0)
                    .OrderByDescending(e => e.TimeMin).ToList();
                foreach (var evt in nonSipEvents)
                {
                    if (actualTotal <= targetCarbs + tolerance) break;
                    double afterRemoval = actualTotal - evt.CarbsInEvent;
                    if (afterRemoval < targetCarbs * 0.90) continue; // Don't drop below 90%
                    actualTotal -= evt.CarbsInEvent;
                    plan.Remove(evt);
                }
            }

            RecalculateCumulativeTotals(plan, products);
        }

        // Undershoot: add gels for large deficits, sip events for small ones
        actualTotal = plan.Sum(e => e.CarbsInEvent);
        if (actualTotal < targetCarbs - tolerance)
        {
            double deficit = targetCarbs - actualTotal;
            var existingTimes = plan.Select(e => e.TimeMin).ToHashSet();
            var lastPhase = phases.LastOrDefault()?.Phase ?? RacePhase.Run;
            var drinkProduct = products.FirstOrDefault(p => p.Texture == ProductTexture.Drink && p.CarbsG > 20);
            var gelProduct = products.FirstOrDefault(p => p.Texture == ProductTexture.Gel || p.Texture == ProductTexture.LightGel);
            int stepSize = Math.Max(8, durationMinutes / 20);

            int attempts = 0;
            for (int t = 10; t < durationMinutes - 5 && deficit > 3 && attempts < 30; t += stepSize)
            {
                if (existingTimes.Contains(t)) continue;

                double hour = t / 60.0;
                var phaseSegment = phases.FirstOrDefault(p => hour >= p.StartHour && hour < p.EndHour)
                                ?? phases.LastOrDefault();
                if (phaseSegment?.Phase == RacePhase.Swim) continue;
                var phase = phaseSegment?.Phase ?? lastPhase;

                // Use gels for large deficits, sips for small ones
                if (gelProduct != null && deficit >= gelProduct.CarbsG * 0.8)
                {
                    plan.Add(new NutritionEvent(
                        TimeMin: t, Phase: phase,
                        PhaseDescription: GetPhaseDescription(phase),
                        ProductName: gelProduct.Name,
                        AmountPortions: 1,
                        Action: GetAction(gelProduct.Texture),
                        TotalCarbsSoFar: 0,
                        CarbsInEvent: gelProduct.CarbsG
                    ));
                    deficit -= gelProduct.CarbsG;
                    existingTimes.Add(t);
                }
                else if (drinkProduct != null)
                {
                    double drinkVolume = drinkProduct.VolumeMl > 0 ? drinkProduct.VolumeMl : SchedulingConstraints.DefaultDrinkVolumeMl;
                    double carbsPerMl = drinkProduct.CarbsG / drinkVolume;
                    double sipMl = SchedulingConstraints.SipVolumeMl;
                    double carbsPerSip = carbsPerMl * sipMl;
                    double actualCarbs = Math.Min(carbsPerSip, deficit);
                    double actualMl = actualCarbs / carbsPerMl;

                    plan.Add(new NutritionEvent(
                        TimeMin: t, Phase: phase,
                        PhaseDescription: GetPhaseDescription(phase),
                        ProductName: drinkProduct.Name,
                        AmountPortions: Math.Round(actualMl / drinkVolume, 2),
                        Action: "Sip",
                        TotalCarbsSoFar: 0,
                        CarbsInEvent: Math.Round(actualCarbs, 1),
                        SipMl: Math.Round(actualMl, 0)
                    ));
                    deficit -= actualCarbs;
                    existingTimes.Add(t);
                }
                attempts++;
            }

            RecalculateCumulativeTotals(plan, products);
        }
    }

    // ─── Product scoring & selection ──────────────────────────────

    private static ProductEnhanced? SelectBestProduct(
        List<ProductEnhanced> products,
        RacePhase segment,
        MultiNutrientTargets remainingNeeds,
        PlannerState state,
        double raceProgressPercent,
        bool caffeineEnabled)
    {
        bool caffeineAllowedNow = caffeineEnabled && raceProgressPercent >= SchedulingConstraints.CaffeinePreferredStartPercent;

        var candidates = products
            .Where(p => p.Texture != ProductTexture.Bake || segment == RacePhase.Bike)
            .Where(p => !p.HasCaffeine || caffeineAllowedNow)
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

        if (caffeineAllowedNow)
        {
            var caffeineOptions = candidates.Where(c => c.Product.HasCaffeine).ToList();
            if (caffeineOptions.Any() && state.TotalCaffeineMg < remainingNeeds.CaffeineMg)
                return caffeineOptions.First().Product;
        }

        return candidates.First().Product;
    }

    private static double ScoreProduct(
        ProductEnhanced product,
        RacePhase segment,
        MultiNutrientTargets remainingNeeds,
        PlannerState state,
        double raceProgressPercent)
    {
        double score = 0.0;
        score += product.CarbsG * 2.0;
        score += GetSegmentSuitabilityScore(product, segment);

        if (remainingNeeds.SodiumMg > 0)
        {
            double sodiumFit = Math.Min(product.SodiumMg / remainingNeeds.SodiumMg, 1.0);
            score += sodiumFit * 15;
        }

        if (remainingNeeds.CaffeineMg > 0 && raceProgressPercent >= SchedulingConstraints.CaffeinePreferredStartPercent)
        {
            if (product.HasCaffeine)
            {
                double caffeineBonus = CalculateCaffeineWindowBonus(raceProgressPercent);
                if (product.CaffeineMg >= 50 && product.CaffeineMg <= 100)
                    score += 25 + caffeineBonus;
                else if (product.CaffeineMg > 0)
                    score += 10 + (caffeineBonus * 0.5);
            }
        }

        int consecutiveUses = state.GetConsecutiveUseCount(product.Name);
        if (consecutiveUses >= 2)
            score -= consecutiveUses * 15;

        double intakesPerHour = state.TotalIntakes / (raceProgressPercent + 0.1);
        if (intakesPerHour > SchedulingConstraints.MaxIntakesPerHour)
            score -= 10;

        return score;
    }

    private static double GetSegmentSuitabilityScore(ProductEnhanced product, RacePhase segment)
    {
        return segment switch
        {
            RacePhase.Bike => product.Texture switch
            {
                ProductTexture.Drink when product.CarbsG > 30 => 50,
                ProductTexture.Drink => 30,
                ProductTexture.Bake => 20,
                ProductTexture.Chew => 15,
                ProductTexture.Gel => 10,
                ProductTexture.LightGel => 5,
                _ => 0
            },
            RacePhase.Run => product.Texture switch
            {
                ProductTexture.Gel when IsIsotonic(product) => 40,
                ProductTexture.Gel => 25,
                ProductTexture.LightGel => 20,
                ProductTexture.Drink when product.VolumeMl <= 200 => 15,
                ProductTexture.Drink => 5,
                ProductTexture.Bake => -30,
                ProductTexture.Chew => -10,
                _ => 0
            },
            RacePhase.Swim => product.Texture switch
            {
                ProductTexture.Gel when IsIsotonic(product) => 20,
                ProductTexture.Gel => 10,
                _ => -20
            },
            _ => 0
        };
    }

    private static bool IsIsotonic(ProductEnhanced product)
    {
        if (product.VolumeMl > 0)
        {
            double carbPercent = (product.CarbsG / (product.VolumeMl / 1000.0)) / 10.0;
            return carbPercent >= 6 && carbPercent <= 8;
        }
        return product.ProductType?.Contains("isotonic", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private static double CalculateCaffeineWindowBonus(double raceProgressPercent)
    {
        if (raceProgressPercent >= SchedulingConstraints.CaffeineOptimalWindow1Start &&
            raceProgressPercent <= SchedulingConstraints.CaffeineOptimalWindow1End)
            return 15.0;
        if (raceProgressPercent >= SchedulingConstraints.CaffeineOptimalWindow2Start &&
            raceProgressPercent <= SchedulingConstraints.CaffeineOptimalWindow2End)
            return 20.0;
        if (raceProgressPercent >= SchedulingConstraints.CaffeineOptimalWindow3Start &&
            raceProgressPercent <= SchedulingConstraints.CaffeineOptimalWindow3End)
            return 25.0;
        if (raceProgressPercent >= SchedulingConstraints.CaffeinePreferredStartPercent)
            return 5.0;
        return 0.0;
    }

    private static ProductEnhanced? SelectExtraProduct(RaceMode mode, List<ProductEnhanced> products) =>
        mode switch
        {
            RaceMode.Cycling => products.FirstOrDefault(p => p.Texture == ProductTexture.Chew || p.Texture == ProductTexture.Bake),
            _ => products.FirstOrDefault(p => p.Texture == ProductTexture.Gel)
        };

    // ─── Helpers ──────────────────────────────────────────────────

    private enum RaceMode { Running, Cycling, TriathlonHalf, TriathlonFull }

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

    private static List<PhaseSegment> BuildPhaseTimeline(SportType sportType, double totalHours)
    {
        if (sportType == SportType.Triathlon)
        {
            const double swimPercent = 0.20;
            const double bikePercent = 0.50;

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
        int raceSafetyMarginMinutes = 5;
        int maxTimeMin = durationMinutes - raceSafetyMarginMinutes;
        bool isTriathlon = phases.Count > 1 && phases.Any(p => p.Phase == RacePhase.Bike);

        var transitionMargins = new Dictionary<RacePhase, int>();
        if (isTriathlon)
        {
            var bikePhase = phases.FirstOrDefault(p => p.Phase == RacePhase.Bike);
            if (bikePhase != null)
            {
                var bikeEndMin = (int)(bikePhase.EndHour * 60);
                transitionMargins[RacePhase.Bike] = bikeEndMin - AdvancedNutritionConfig.BikeToRunTransitionMarginMin;
            }
        }

        int currentTime = slotInterval;

        while (currentTime < maxTimeMin)
        {
            double hour = currentTime / 60.0;
            var currentPhase = phases.FirstOrDefault(p => hour >= p.StartHour && hour < p.EndHour);
            if (currentPhase == null && phases.Any())
            {
                var lastPhase = phases.Last();
                if (hour >= lastPhase.StartHour && hour <= lastPhase.EndHour)
                    currentPhase = lastPhase;
            }

            if (currentPhase != null)
            {
                if (transitionMargins.ContainsKey(currentPhase.Phase) &&
                    currentTime >= transitionMargins[currentPhase.Phase])
                {
                    currentTime += slotInterval;
                    continue;
                }
                slots.Add(new Slot(currentTime, currentPhase.Phase));
            }

            if (isTriathlon)
            {
                if (currentPhase?.Phase == RacePhase.Bike)
                    currentTime += 18;
                else if (currentPhase?.Phase == RacePhase.Run)
                    currentTime += 25;
                else
                    currentTime += slotInterval;
            }
            else
            {
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
        return new PlannerState { NextCaffeineHour = startHour };
    }

    private static int? FindAvailableTimeSlot(int startMin, int endMin, List<int> existingTimes, int minSpacingMin)
    {
        if (!existingTimes.Any())
            return startMin + 5;

        var existingSet = existingTimes.ToHashSet();
        var sortedTimes = existingTimes.OrderBy(t => t).ToList();

        for (int i = 0; i < sortedTimes.Count - 1; i++)
        {
            int gap = sortedTimes[i + 1] - sortedTimes[i];
            if (gap >= minSpacingMin * 2)
            {
                int proposedTime = sortedTimes[i] + (gap / 2);
                if (!existingSet.Contains(proposedTime))
                    return proposedTime;
            }
        }

        if (sortedTimes[0] - startMin >= minSpacingMin)
        {
            int proposedTime = Math.Max(startMin + 5, sortedTimes[0] - minSpacingMin);
            if (!existingSet.Contains(proposedTime))
                return proposedTime;
        }

        if (endMin - sortedTimes[^1] >= minSpacingMin)
        {
            int proposedTime = Math.Min(sortedTimes[^1] + minSpacingMin, endMin - 5);
            if (!existingSet.Contains(proposedTime))
                return proposedTime;
        }

        for (int time = startMin; time <= endMin; time++)
        {
            if (!existingSet.Contains(time))
                return time;
        }

        return null;
    }

    private static string GetAction(ProductTexture texture) =>
        texture switch
        {
            ProductTexture.Gel or ProductTexture.LightGel => "Squeeze",
            ProductTexture.Drink => "Sip",
            ProductTexture.Chew => "Chew",
            ProductTexture.Bake => "Eat",
            _ => "Consume"
        };

    // ─── Validation ───────────────────────────────────────────────

    private sealed record ValidationResult(
        List<NutritionEvent> Plan,
        List<string> Warnings,
        List<string> Errors
    );

    private static ValidationResult ValidateAndAutoFix(
        List<NutritionEvent> plan,
        MultiNutrientTargets targets,
        List<ProductEnhanced> products,
        int durationMinutes,
        bool caffeineEnabled)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        double calculatedCarbs = plan.Sum(e => e.CarbsInEvent);
        double calculatedCaffeine = plan.Where(e => e.HasCaffeine && e.CaffeineMg.HasValue).Sum(e => e.CaffeineMg!.Value);

        // Target consistency
        double carbTolerance = targets.CarbsG * SchedulingConstraints.TargetTolerancePercent;
        if (calculatedCarbs < targets.CarbsG - carbTolerance)
            warnings.Add($"Plan underdelivers carbs: {calculatedCarbs:F0}g < {targets.CarbsG:F0}g target (-{targets.CarbsG - calculatedCarbs:F0}g)");
        else if (calculatedCarbs > targets.CarbsG + carbTolerance)
            warnings.Add($"Plan overdelivers carbs: {calculatedCarbs:F0}g > {targets.CarbsG:F0}g target (+{calculatedCarbs - targets.CarbsG:F0}g)");

        // Spacing validation (non-sip events only)
        var nonSipEvents = plan.Where(e => e.Action != "Sip").OrderBy(e => e.TimeMin).ToList();
        for (int i = 0; i < nonSipEvents.Count - 1; i++)
        {
            var evt1 = nonSipEvents[i];
            var evt2 = nonSipEvents[i + 1];
            int spacing = evt2.TimeMin - evt1.TimeMin;

            if (spacing < SchedulingConstraints.ClusterWindow)
                errors.Add($"Clustering detected: items at {evt1.TimeMin}min and {evt2.TimeMin}min too close ({spacing}min < {SchedulingConstraints.ClusterWindow}min)");
        }

        // Caffeine validation
        if (!caffeineEnabled && calculatedCaffeine > 0)
            errors.Add($"Caffeine disabled but plan contains {calculatedCaffeine:F0}mg");
        else if (caffeineEnabled && calculatedCaffeine > targets.CaffeineMg * 1.2)
            warnings.Add($"Caffeine high: {calculatedCaffeine:F0}mg > {targets.CaffeineMg:F0}mg target");

        // Product diversity
        var productCounts = plan.GroupBy(e => e.ProductName).ToDictionary(g => g.Key, g => g.Count());
        if (productCounts.Any())
        {
            // Only count non-sip events for diversity check
            var nonSipCounts = plan.Where(e => e.Action != "Sip").GroupBy(e => e.ProductName).ToDictionary(g => g.Key, g => g.Count());
            if (nonSipCounts.Any())
            {
                var mostCommon = nonSipCounts.MaxBy(kvp => kvp.Value);
                var nonSipTotal = plan.Count(e => e.Action != "Sip");
                if (nonSipTotal > 2 && mostCommon.Value > nonSipTotal * 0.7)
                    warnings.Add($"Low diversity: {mostCommon.Key} used {mostCommon.Value} times ({mostCommon.Value * 100 / nonSipTotal}% of non-sip events)");
            }
        }

        // Hydration coupling check
        var hydrationWarnings = CheckHydrationCoupling(plan, products);
        warnings.AddRange(hydrationWarnings);

        return new ValidationResult(plan, warnings, errors);
    }

    private static int GetMinimumSpacing(ProductEnhanced product1, ProductEnhanced product2, RacePhase phase)
    {
        if (product1.HasCaffeine || product2.HasCaffeine)
            return SchedulingConstraints.MinCaffeineSpacing;

        bool isSolid1 = product1.Texture == ProductTexture.Bake || product1.Texture == ProductTexture.Chew;
        bool isSolid2 = product2.Texture == ProductTexture.Bake || product2.Texture == ProductTexture.Chew;
        if (isSolid1 || isSolid2)
            return phase == RacePhase.Bike ? SchedulingConstraints.MinSolidSpacingBike : SchedulingConstraints.MinSolidSpacingRun;

        bool isGel1 = product1.Texture == ProductTexture.Gel || product1.Texture == ProductTexture.LightGel;
        bool isGel2 = product2.Texture == ProductTexture.Gel || product2.Texture == ProductTexture.LightGel;
        if (isGel1 || isGel2)
            return phase == RacePhase.Bike ? SchedulingConstraints.MinGelSpacingBike : SchedulingConstraints.MinGelSpacingRun;

        return SchedulingConstraints.MinDrinkSpacing;
    }

    private static List<string> CheckHydrationCoupling(List<NutritionEvent> plan, List<ProductEnhanced> products)
    {
        var warnings = new List<string>();
        const int hydrationWindowMin = 10;

        for (int i = 0; i < plan.Count; i++)
        {
            var evt = plan[i];
            var product = products.FirstOrDefault(p => p.Name == evt.ProductName);
            if (product == null) continue;

            bool isNonIsotonicGel = (product.Texture == ProductTexture.Gel || product.Texture == ProductTexture.LightGel)
                                    && !IsIsotonic(product);
            if (!isNonIsotonicGel) continue;

            bool hasNearbyHydration = false;
            for (int j = 0; j < plan.Count; j++)
            {
                if (j == i) continue;
                var other = plan[j];
                if (Math.Abs(other.TimeMin - evt.TimeMin) <= hydrationWindowMin &&
                    (other.Action == "Sip" || other.SipMl > 0))
                {
                    hasNearbyHydration = true;
                    break;
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
}
