# 🔬 Temperature Algorithm - Code Deep Dive

## Source Code Overview

Located in: `src/RaceDay.Core/NutritionCalculator.cs`

---

## Complete Algorithm Implementation

### Main Entry Point

```csharp
public static NutritionTargets CalculateTargets(RaceProfile race, AthleteProfile athlete)
{
    // Step 1: Calculate each nutrient independently
    double carbs = CalculateCarbohydrates(race);      // No temp effect
    double fluids = CalculateFluids(race, athlete);   // TEMP MATTERS ✅
    double sodium = CalculateSodium(race, athlete);   // TEMP MATTERS ✅

    // Return all three targets
    return new NutritionTargets(carbs, fluids, sodium);
}
```

---

## Step 1: Carbohydrate Calculation

```csharp
private static double CalculateCarbohydrates(RaceProfile race)
{
    // STEP 1: Base on intensity (NO temperature consideration)
    double carbs = race.Intensity switch
    {
        IntensityLevel.Easy      => NutritionConstants.Carbohydrates.EasyIntensity,      // 50
        IntensityLevel.Moderate  => NutritionConstants.Carbohydrates.ModerateIntensity,  // 70
        IntensityLevel.Hard      => NutritionConstants.Carbohydrates.HardIntensity,      // 90
        _ => NutritionConstants.Carbohydrates.ModerateIntensity  // Default 70
    };

    // STEP 2: Add bonus for long races (if not easy)
    if (race.DurationHours > NutritionConstants.Carbohydrates.LongRaceDurationThreshold  // > 5 hours
        && race.Intensity != IntensityLevel.Easy)
    {
        carbs += NutritionConstants.Carbohydrates.LongRaceBonus;  // +10
    }

    // STEP 3: Return (TEMPERATURE IS COMPLETELY IGNORED)
    return carbs;
}

// EXAMPLES:
// Easy,     2 hours, 15°C:   50 g/hr  (no bonus, < 5 hrs)
// Hard,     3 hours, 30°C:   90 g/hr  (no bonus, < 5 hrs, temp ignored)
// Hard,     6 hours, 3°C:   100 g/hr  (90 + 10 bonus, temp ignored!)
```

---

## Step 2: Fluid Calculation (Temperature Critical)

```csharp
private static double CalculateFluids(RaceProfile race, AthleteProfile athlete)
{
    // STEP 1: Start with baseline
    double fluids = NutritionConstants.Fluids.BaseIntake;  // 500 ml/hr
    
    // STEP 2: Apply TEMPERATURE ADJUSTMENT (KEY PART!)
    if (race.TemperatureC >= NutritionConstants.Temperature.HotThreshold)  // >= 25°C
    {
        fluids += NutritionConstants.Fluids.HotWeatherBonus;  // +200
        // Logic: Higher temp → more sweating → more fluid loss
    }
    
    if (race.TemperatureC <= NutritionConstants.Temperature.ColdThreshold)  // <= 5°C
    {
        fluids -= NutritionConstants.Fluids.ColdWeatherPenalty;  // -100
        // Logic: Lower temp → less sweating → less fluid needed
    }
    // Note: Between 5-25°C, temperature has NO effect

    // STEP 3: Apply WEIGHT ADJUSTMENT
    if (athlete.WeightKg > NutritionConstants.Weight.HeavyAthleteThreshold)  // > 80 kg
    {
        fluids += NutritionConstants.Fluids.HeavyAthleteBonus;  // +50
        // Logic: Heavier athletes have higher metabolic rate
    }
    
    if (athlete.WeightKg < NutritionConstants.Weight.LightAthleteThreshold)  // < 60 kg
    {
        fluids -= NutritionConstants.Fluids.LightAthletePenalty;  // -50
        // Logic: Lighter athletes have lower metabolic rate
    }

    // STEP 4: Safety clamp to safe physiological limits
    fluids = Math.Clamp(fluids, 
        NutritionConstants.Fluids.MinIntake,      // 300 ml/hr minimum
        NutritionConstants.Fluids.MaxIntake);     // 900 ml/hr maximum
    
    return fluids;
}

// EXAMPLES:
// 75 kg, 3°C, moderate:     500 - 100 = 400 ml/hr
// 75 kg, 15°C, moderate:    500 + 0 = 500 ml/hr
// 75 kg, 30°C, moderate:    500 + 200 = 700 ml/hr
// 85 kg, 30°C, moderate:    500 + 200 + 50 = 750 ml/hr
// 55 kg, 30°C, moderate:    500 + 200 - 50 = 650 ml/hr
// 100 kg, 35°C, moderate:   500 + 200 + 50 = 750 ml/hr (capped at 900 max)
// 40 kg, -5°C, moderate:    500 - 100 - 50 = 350 → clamped to 300 ml/hr
```

---

## Step 3: Sodium Calculation (Temperature Effect)

```csharp
private static double CalculateSodium(RaceProfile race, AthleteProfile athlete)
{
    // STEP 1: Start with baseline
    double sodium = NutritionConstants.Sodium.BaseIntake;  // 400 mg/hr

    // STEP 2: Apply TEMPERATURE ADJUSTMENT (ONLY for HOT weather)
    if (race.TemperatureC >= NutritionConstants.Temperature.HotThreshold)  // >= 25°C
    {
        sodium += NutritionConstants.Sodium.HotWeatherBonus;  // +200
        // Logic: Higher temp → more sweating → more salt loss
    }
    
    // NOTE: Cold weather has NO effect on sodium
    // Why? Sodium loss is primarily through sweat, not temperature-dependent
    // Cold reduces sweat, but doesn't increase it, so no penalty applies

    // STEP 3: Apply WEIGHT ADJUSTMENT
    if (athlete.WeightKg > NutritionConstants.Weight.HeavyAthleteThreshold)  // > 80 kg
    {
        sodium += NutritionConstants.Sodium.HeavyAthleteBonus;  // +100
        // Logic: Heavier athletes sweat more (more surface area, higher mass)
    }
    
    // NOTE: No penalty for light athletes
    // Why? Already at baseline, no reduction needed

    // STEP 4: Safety clamp to safe physiological limits
    sodium = Math.Clamp(sodium, 
        NutritionConstants.Sodium.MinIntake,      // 300 mg/hr minimum
        NutritionConstants.Sodium.MaxIntake);     // 1000 mg/hr maximum
    
    return sodium;
}

// EXAMPLES:
// 75 kg, 3°C, moderate:     400 + 0 = 400 mg/hr (cold = no effect)
// 75 kg, 15°C, moderate:    400 + 0 = 400 mg/hr
// 75 kg, 30°C, moderate:    400 + 200 = 600 mg/hr
// 85 kg, 30°C, moderate:    400 + 200 + 100 = 700 mg/hr
// 55 kg, 30°C, moderate:    400 + 200 + 0 = 600 mg/hr (no penalty)
// 100 kg, 35°C, moderate:   400 + 200 + 100 = 700 mg/hr
// 90 kg, 2°C, moderate:     400 + 0 + 100 = 500 mg/hr (weight bonus applies!)
```

---

## Constants Definition

Located in: `src/RaceDay.Core/NutritionConstants.cs`

```csharp
public static class NutritionConstants
{
    public static class Temperature
    {
        // Temperature thresholds in Celsius
        public const double HotThreshold = 25;   // At or above = HOT
        public const double ColdThreshold = 5;   // At or below = COLD
        // Between 5-25°C = MODERATE (no adjustments)
    }

    public static class Fluids
    {
        public const double BaseIntake = 500;              // ml/hr baseline
        public const double HotWeatherBonus = 200;         // ml/hr added when temp >= 25°C
        public const double ColdWeatherPenalty = 100;      // ml/hr subtracted when temp <= 5°C
        public const double HeavyAthleteBonus = 50;        // ml/hr for weight > 80 kg
        public const double LightAthletePenalty = 50;      // ml/hr for weight < 60 kg
        public const double MinIntake = 300;               // ml/hr minimum safety limit
        public const double MaxIntake = 900;               // ml/hr maximum safety limit
    }

    public static class Sodium
    {
        public const double BaseIntake = 400;              // mg/hr baseline
        public const double HotWeatherBonus = 200;         // mg/hr added when temp >= 25°C
        // NOTE: No cold penalty (cold doesn't increase sweat loss)
        public const double HeavyAthleteBonus = 100;       // mg/hr for weight > 80 kg
        // NOTE: No penalty for light athletes
        public const double MinIntake = 300;               // mg/hr minimum safety limit
        public const double MaxIntake = 1000;              // mg/hr maximum safety limit
    }

    public static class Carbohydrates
    {
        public const double EasyIntensity = 50;            // g/hr for easy pace
        public const double ModerateIntensity = 70;        // g/hr for moderate pace
        public const double HardIntensity = 90;            // g/hr for hard pace
        public const double LongRaceBonus = 10;            // g/hr added if duration > 5 hrs
        public const double LongRaceDurationThreshold = 5; // hours
        // NOTE: TEMPERATURE IS IGNORED
    }

    public static class Weight
    {
        public const double HeavyAthleteThreshold = 80;    // kg, above = gets bonus
        public const double LightAthleteThreshold = 60;    // kg, below = gets penalty
    }
}
```

---

## Flow Diagram: Temperature Processing

```
Input: RaceProfile (including TemperatureC)
    ↓
    ├─→ CalculateCarbohydrates()
    │   ├─ Check intensity
    │   ├─ Check duration > 5 hrs
    │   └─ IGNORE temperature ❌
    │       Return: 50/70/90 g/hr (+ bonus if long)
    │
    ├─→ CalculateFluids()
    │   ├─ Start: 500 ml/hr
    │   ├─ if temp >= 25:  +200  ✅ Temperature critical
    │   ├─ if temp <= 5:   -100  ✅ Temperature critical
    │   ├─ if weight > 80:  +50
    │   ├─ if weight < 60:  -50
    │   └─ Clamp [300, 900]
    │       Return: 300-900 ml/hr
    │
    └─→ CalculateSodium()
        ├─ Start: 400 mg/hr
        ├─ if temp >= 25:  +200  ✅ Temperature effect
        ├─ IGNORE if temp <= 5:  ✅ No cold effect
        ├─ if weight > 80:  +100
        └─ Clamp [300, 1000]
            Return: 300-1000 mg/hr

Output: NutritionTargets(carbs, fluids, sodium)
```

---

## Real Code Example: Hot vs Cold Race

```csharp
// SCENARIO 1: Cold Race
var raceProfile = new RaceProfile(
    SportType: SportType.Triathlon,
    DurationHours: 3.75,
    TemperatureC: 3,        // ← COLD (≤ 5)
    Intensity: IntensityLevel.Hard
);
var athlete = new AthleteProfile(WeightKg: 75);

var targets = NutritionCalculator.CalculateTargets(raceProfile, athlete);
// targets.CarbsGPerHour = 90 (no temp effect)
// targets.FluidsMlPerHour = 400 (500 - 100 for cold)
// targets.SodiumMgPerHour = 400 (400 + 0 for cold)


// SCENARIO 2: Hot Race (SAME other parameters)
var hotRaceProfile = new RaceProfile(
    SportType: SportType.Triathlon,
    DurationHours: 3.75,
    TemperatureC: 30,       // ← HOT (≥ 25)
    Intensity: IntensityLevel.Hard
);

var hotTargets = NutritionCalculator.CalculateTargets(hotRaceProfile, athlete);
// targets.CarbsGPerHour = 90 (no temp effect - SAME!)
// targets.FluidsMlPerHour = 700 (500 + 200 for hot) ← 75% MORE!
// targets.SodiumMgPerHour = 600 (400 + 200 for hot) ← 50% MORE!
```

---

## Temperature Boundary Conditions

```csharp
// Testing exact threshold values
var coldBoundary = new RaceProfile(
    SportType: SportType.Run,
    DurationHours: 2.0,
    TemperatureC: 5,        // EXACTLY at cold threshold
    Intensity: IntensityLevel.Hard
);
// At temp = 5°C, condition is: temp <= 5 → TRUE
// So penalty applies: 500 - 100 = 400 ml/hr ✓

var hotBoundary = new RaceProfile(
    SportType: SportType.Run,
    DurationHours: 2.0,
    TemperatureC: 25,       // EXACTLY at hot threshold
    Intensity: IntensityLevel.Hard
);
// At temp = 25°C, condition is: temp >= 25 → TRUE
// So bonus applies: 500 + 200 = 700 ml/hr ✓

var moderate1 = new RaceProfile(
    SportType: SportType.Run,
    DurationHours: 2.0,
    TemperatureC: 5.1,      // Just above cold threshold
    Intensity: IntensityLevel.Hard
);
// 5.1 > 5 AND 5.1 < 25 → NO adjustments
// Result: 500 ml/hr (baseline) ✓

var moderate2 = new RaceProfile(
    SportType: SportType.Run,
    DurationHours: 2.0,
    TemperatureC: 24.9,     // Just below hot threshold
    Intensity: IntensityLevel.Hard
);
// 24.9 < 25 AND 24.9 > 5 → NO adjustments
// Result: 500 ml/hr (baseline) ✓
```

---

## Safety Clamp Demonstration

```csharp
// Extreme case: Very light athlete in extreme cold
var extremeColdProfile = new RaceProfile(
    SportType: SportType.Run,
    DurationHours: 2.0,
    TemperatureC: -10,      // Extremely cold
    Intensity: IntensityLevel.Easy
);
var lightAthlete = new AthleteProfile(WeightKg: 45);

// Manual calculation:
// Start: 500 ml/hr
// Cold penalty (-10 ≤ 5): 500 - 100 = 400 ml/hr
// Light athlete (<60): 400 - 50 = 350 ml/hr
// CLAMP to [300, 900]: 350 ml/hr ✓ (within bounds)

var fluids = CalculateFluids(extremeColdProfile, lightAthlete);
// Result: 350 ml/hr (clamped but not hit minimum)


// Edge case: Maximum fluids scenario
var extremeHotProfile = new RaceProfile(
    SportType: SportType.Triathlon,
    DurationHours: 5.5,     // Long race
    TemperatureC: 40,       // Extreme heat
    Intensity: IntensityLevel.Hard
);
var heavyAthlete = new AthleteProfile(WeightKg: 100);

// Manual calculation:
// Start: 500 ml/hr
// Hot bonus (40 ≥ 25): 500 + 200 = 700 ml/hr
// Heavy athlete (>80): 700 + 50 = 750 ml/hr
// CLAMP to [300, 900]: 750 ml/hr ✓ (within bounds)

var fluidExtreme = CalculateFluids(extremeHotProfile, heavyAthlete);
// Result: 750 ml/hr (not hitting maximum)

// If hypothetically got 950 ml/hr from calculation:
// CLAMP would reduce to: 900 ml/hr (hit maximum!)
```

---

## Unit Test Examples

```csharp
[Fact]
public void CalculateCarbohydrates_IgnoresTemperature()
{
    // Arrange
    var coldRace = new RaceProfile(SportType.Run, 2.5, 3, IntensityLevel.Hard);
    var hotRace = new RaceProfile(SportType.Run, 2.5, 35, IntensityLevel.Hard);
    
    // Act
    var coldCarbs = NutritionCalculator.CalculateCarbohydrates(coldRace);
    var hotCarbs = NutritionCalculator.CalculateCarbohydrates(hotRace);
    
    // Assert
    Assert.Equal(90, coldCarbs);  // Hard intensity
    Assert.Equal(90, hotCarbs);   // SAME! Temperature ignored ✓
}

[Fact]
public void CalculateFluids_IncreaseInHotWeather()
{
    // Arrange
    var athlete = new AthleteProfile(75);
    var coldRace = new RaceProfile(SportType.Triathlon, 3.75, 3, IntensityLevel.Hard);
    var hotRace = new RaceProfile(SportType.Triathlon, 3.75, 30, IntensityLevel.Hard);
    
    // Act
    var coldFluids = NutritionCalculator.CalculateFluids(coldRace, athlete);
    var hotFluids = NutritionCalculator.CalculateFluids(hotRace, athlete);
    
    // Assert
    Assert.Equal(400, coldFluids);    // 500 - 100 for cold
    Assert.Equal(700, hotFluids);     // 500 + 200 for hot ✓
}

[Fact]
public void CalculateSodium_IncreaseInHotWeather_NoChangeInCold()
{
    // Arrange
    var athlete = new AthleteProfile(75);
    var coldRace = new RaceProfile(SportType.Run, 2.0, 3, IntensityLevel.Hard);
    var hotRace = new RaceProfile(SportType.Run, 2.0, 30, IntensityLevel.Hard);
    
    // Act
    var coldSodium = NutritionCalculator.CalculateSodium(coldRace, athlete);
    var hotSodium = NutritionCalculator.CalculateSodium(hotRace, athlete);
    
    // Assert
    Assert.Equal(400, coldSodium);    // Baseline (cold ignored)
    Assert.Equal(600, hotSodium);     // 400 + 200 for hot ✓
}
```

---

## Performance Notes

```csharp
// All calculations are simple arithmetic:
// - No loops or complex logic
// - Time complexity: O(1)
// - Space complexity: O(1)
// - Typical execution: < 1 microsecond

public static NutritionTargets CalculateTargets(RaceProfile race, AthleteProfile athlete)
{
    // Total time: ~0.001 ms
    var carbs = CalculateCarbohydrates(race);      // ~0.0003 ms (one switch statement)
    var fluids = CalculateFluids(race, athlete);   // ~0.0003 ms (few comparisons)
    var sodium = CalculateSodium(race, athlete);   // ~0.0003 ms (few comparisons)
    
    // Return pre-built record (immutable)
    return new NutritionTargets(carbs, fluids, sodium);
}
```

---

## Summary: Temperature in Code

| Metric | Calculation | Temperature Effect | Code Location |
|--------|-------------|-------------------|---------------|
| **Carbs** | Intensity + Duration | ❌ NONE | `CalculateCarbohydrates()` |
| **Fluids** | Base ± Temp ± Weight | ✅ ±200 ml/hr | `CalculateFluids()` |
| **Sodium** | Base ± Temp ± Weight | ✅ ±200 mg/hr (hot only) | `CalculateSodium()` |

Temperature thresholds defined in: `NutritionConstants.Temperature`
- Hot: ≥ 25°C
- Cold: ≤ 5°C
- Moderate: 5°C < temp < 25°C

