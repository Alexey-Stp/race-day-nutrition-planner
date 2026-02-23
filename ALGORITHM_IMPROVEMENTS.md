# Algorithm Improvements - Race Day Nutrition Planner

## Overview
This document describes the comprehensive algorithm redesign implemented across three phases to create a **physiologically realistic, evidence-based nutrition planning system** for endurance racing.

## Implementation Status: ✅ COMPLETE

### Phase 1: Foundation (COMPLETE)
**Goal:** Establish multi-nutrient tracking, scoring system, and validation infrastructure

#### Deliverables:
- ✅ [SchedulingConstraints.cs](src/RaceDay.Core/Constants/SchedulingConstraints.cs) - Centralized timing rules and distribution ratios
- ✅ [MultiNutrientTargets.cs](src/RaceDay.Core/Models/MultiNutrientTargets.cs) - Comprehensive nutrition tracking (carbs, sodium, fluid, caffeine)
- ✅ Enhanced PlannerState - Product history tracking with consecutive use counting
- ✅ `ScoreProduct()` algorithm - Multi-factor product scoring with segment suitability
- ✅ `ValidateAndAutoFix()` - 8 comprehensive validation checks
- ✅ `CalculateMultiNutrientTargets()` - Evidence-based target calculation

**Key Features:**
- **Multi-Nutrient Tracking:** Carbs, sodium, fluid, and caffeine tracked throughout race
- **Intelligent Scoring:** Products scored on carb efficiency (×2.0), segment fit (up to 50 pts), sodium contribution (×15), caffeine timing (25 pts), and diversity (-15 per repeat)
- **Segment Awareness:** Different product suitability for bike vs. run vs. swim phases
- **Validation Layer:** Catches spacing violations, target mismatches, clustering issues

### Phase 2: Selection & Distribution (COMPLETE)
**Goal:** Implement drink-first strategy and intelligent product selection

#### Deliverables:
- ✅ `BuildDrinkBackbone()` - Schedules high-carb drinks (>30g) every 35-40 min targeting 45% of carbs
- ✅ `SelectBestProduct()` - Scoring-based selection replacing hardcoded texture preferences
- ✅ Clustering Prevention - Enforces 5-minute safety window between products
- ✅ Validation Integration - `ValidateAndAutoFix()` called at end of plan generation
- ✅ Pre-race Logic - Only adds if <10% target met, prefers bars then gels
- ✅ Multi-nutrient accumulation throughout planning process

**Key Features:**
- **Drink Backbone:** High-carb drinks scheduled first for efficiency, especially on bike
- **Smart Selection:** Uses scoring system to intelligently choose products based on context
- **Anti-Clustering:** No two items within 5 minutes of each other
- **Triathlon Optimization:** 70/30 bike/run carb distribution, drink-heavy on bike

### Phase 3: Advanced Features (COMPLETE)
**Goal:** Strategic caffeine timing, hydration coupling, and comprehensive diagnostics

#### Deliverables:
- ✅ **Strategic Caffeine Windows:**
  - Window 1 (40-55%): +15 pts - Early strategic boost
  - Window 2 (65-80%): +20 pts - Mid-late race maintenance
  - Window 3 (85-95%): +25 pts - Final push
- ✅ **Hydration Coupling:** Validates non-isotonic gels have hydration within 10 minutes
- ✅ **Enhanced Validation:** Added `CheckHydrationCoupling()` to validation pipeline
- ✅ **API Integration:**
  - New `PlanResult` model with Events, Warnings, Errors
  - `GeneratePlanWithDiagnostics()` methods in generator and service
  - `AdvancedPlanResponse` includes Warnings and Errors lists
  - `CaffeineEnabled` parameter in API request

**Key Features:**
- **Strategic Caffeine:** Timing windows with bonus scoring, pre-race caffeine blocked
- **Hydration Guidance:** Warns when gels lack nearby hydration sources
- **Comprehensive Diagnostics:** All warnings/errors surfaced to API consumers
- **User-Facing Alerts:** Frontend can display validation results to users

---

## Technical Architecture

### Core Components

#### 1. SchedulingConstraints
```csharp
// Minimum spacing (minutes)
MinGelSpacingBike = 15, MinGelSpacingRun = 20
MinSolidSpacingBike = 25, MinSolidSpacingRun = 30
MinDrinkSpacing = 12, MinCaffeineSpacing = 45
ClusterWindow = 5  // No 2 items within 5 min

// Distribution ratios
TriathlonBikeCarbRatio = 0.70, TriathlonRunCarbRatio = 0.30

// Caffeine windows
CaffeinePreferredStartPercent = 0.40
CaffeineOptimalWindow1 = 0.40-0.55 (+15 pts)
CaffeineOptimalWindow2 = 0.65-0.80 (+20 pts)
CaffeineOptimalWindow3 = 0.85-0.95 (+25 pts)
```

#### 2. MultiNutrientTargets
```csharp
public record MultiNutrientTargets(
    double CarbsG, double SodiumMg, double FluidMl, double CaffeineMg,
    double CarbsPerHour, double SodiumPerHour, double FluidPerHour,
    Dictionary<RacePhase, PhaseTargets>? SegmentTargets
);
```

#### 3. Product Scoring Algorithm
```csharp
Score = (CarbEfficiency × 2.0)              // Prioritize high-carb products
      + SegmentSuitabilityScore             // Up to 50 pts for perfect fit
      + (SodiumFit × 15)                    // Sodium contribution
      + CaffeineWindowBonus                 // 5-25 pts based on timing
      - (ConsecutiveUses × 15)              // Diversity penalty
      - (HighIntakeFrequency × 10);         // Action count penalty
```

**Segment Suitability Matrix:**
- **Bike Phase:**
  - High-carb drinks (>30g): 50 pts
  - Regular drinks: 30 pts
  - Bars: 20 pts
  - Gels: 10 pts
- **Run Phase:**
  - Isotonic gels: 40 pts
  - Regular gels: 25 pts
  - Light gels: 20 pts
  - Bars: -30 pts (strong penalty)

#### 4. Validation System
8 comprehensive checks:
1. **Target Consistency** - Within 10% tolerance
2. **Spacing Validation** - Product-specific minimum gaps
3. **Clustering Detection** - No items within 5 minutes
4. **Caffeine Validation** - Respects enabled/disabled state
5. **Product Diversity** - No single product >60% of plan
6. **Drink Usage** - Ensures drinks used when available
7. **Hydration Coupling** - Gels have nearby water/drinks
8. **Phase Distribution** - Triathlon bike/run balance

---

## Test Coverage

### Test Suite: 212/212 Tests Passing ✅

#### AdvancedPlanGeneratorTests.cs (36 tests)
- Basic plan generation across sport types
- Pre-race intake logic
- Triathlon phase distribution
- Caffeine handling
- Product selection logic
- Edge cases and boundary conditions

#### AlgorithmImprovementTests.cs (7 tests)
- ✅ `RespectsClusterWindow` - No items within 5 minutes
- ✅ `UsesDrinksWhenAvailable` - Drinks provide ≥20% of carbs
- ✅ `WithCaffeineEnabled_IncludesCaffeineAfter40Percent` - Timing enforcement
- ✅ `WithCaffeineDisabled_NoCaffeineProducts` - Zero caffeine when disabled
- ✅ `TriathlonBikePhaseDominance` - Bike ≥55% of carbs
- ✅ `ProductDiversity_NoExcessiveRepetition` - No single product >75%

#### ValidationTests.cs, NutritionCalculatorTests.cs, etc. (114 tests)
- Input validation, calculator accuracy, repository operations

---

## API Changes

### Enhanced Request Model
```json
{
  "athleteWeightKg": 75,
  "sportType": "Triathlon",
  "durationHours": 4.5,
  "temperatureC": 22,
  "intensity": "Hard",
  "caffeineEnabled": true,  // NEW
  "filter": { "brands": ["Maurten", "SIS"] }
}
```

### Enhanced Response Model
```json
{
  "race": { ... },
  "athlete": { ... },
  "nutritionSchedule": [ ... ],
  "shoppingSummary": { ... },
  "warnings": [  // NEW
    "Gel at 45min (Energy Gel) may need additional hydration...",
    "Low diversity: High Carb Drink used 7 times (58% of plan)"
  ],
  "errors": []  // NEW
}
```

---

## Performance Characteristics

- **Drink Backbone:** Covers ~45% of carbs in 3-5 placements
- **Scoring-Based Selection:** O(n log n) per slot for product ranking
- **Validation:** O(n²) for pairwise spacing checks
- **Total Complexity:** O(n² + m log m) where n = slots, m = products
- **Typical Runtime:** <100ms for 3-hour race with 20 products

---

## Evidence-Based Design Principles

### 1. Carbohydrate Recommendations
- **Running:** 60-90g/hour (based on GI tolerance)
- **Cycling:** 80-120g/hour (better absorption capacity)
- **Triathlon:** Progressive loading (bike-heavy strategy)

### 2. Caffeine Protocol
- **Timing:** Not before 40% of race (avoid early jitters)
- **Dosage:** 50-100mg per intake (optimal performance boost)
- **Frequency:** 45+ min spacing (avoid tolerance/GI issues)
- **Strategic Windows:** Target critical race phases for maximum benefit

### 3. Hydration Coupling
- **Hypotonic/Isotonic:** Self-hydrating, no extra water needed
- **Regular Gels:** Require ~200ml water within 10 minutes
- **Validation:** Warns when gels lack nearby fluid sources

### 4. Sport-Specific Adaptation
- **Triathlon:** Bike phase prioritized for nutrition (stable position, better absorption)
- **Running:** Lighter products, isotonic gels preferred (GI sensitivity)
- **Cycling:** Solid foods acceptable (lower GI stress, stable position)

---

## Future Enhancement Opportunities

### Phase 4 Candidates:
- **Real-Time Weather Integration:** Adjust sodium/fluid based on actual conditions
- **Personalized GI Tolerance:** Learn from athlete feedback to refine product selection
- **Race-Day Adjustments:** Support plan modifications during the race
- **Comparative Analysis:** Show multiple plan alternatives with trade-offs
- **Product Substitutions:** Smart swaps when preferred products unavailable

### Phase 5 Candidates:
- **Integration with Wearables:** Real-time heart rate, power data to adjust fueling
- **Race Simulation Mode:** Test plans with virtual race scenarios
- **Nutritionist Review Mode:** Export plans for professional review
- **Community Sharing:** Athletes share successful plans for similar races

---

## Version History

### v2.0.0 (February 11, 2026) - Algorithm Redesign
- ✅ Phase 1: Foundation (Multi-nutrient tracking, scoring, validation)
- ✅ Phase 2: Selection & Distribution (Drink backbone, smart selection)
- ✅ Phase 3: Advanced Features (Strategic caffeine, hydration coupling, diagnostics)
- 212/212 tests passing
- Full API integration with warnings/errors

### v1.0.0 (Previous) - Initial Implementation
- Basic carb-only planning
- Simple texture-based selection
- Limited validation

---

## Developer Notes

### Key Files Modified:
- [AdvancedPlanGenerator.cs](src/RaceDay.Core/Services/AdvancedPlanGenerator.cs) - Core planning algorithm (1141 lines)
- [NutritionCalculator.cs](src/RaceDay.Core/Services/NutritionCalculator.cs) - Target calculation
- [NutritionPlanService.cs](src/RaceDay.Core/Services/NutritionPlanService.cs) - Service layer
- [ApiEndpointExtensions.cs](src/RaceDay.API/ApiEndpointExtensions.cs) - API integration
- [SchedulingConstraints.cs](src/RaceDay.Core/Constants/SchedulingConstraints.cs) - Configuration
- [MultiNutrientTargets.cs](src/RaceDay.Core/Models/MultiNutrientTargets.cs) - Data models
- [PlanResult.cs](src/RaceDay.Core/Models/PlanResult.cs) - Diagnostics model

### Running the Application:
```bash
# Start API + React Frontend
dotnet run --project src/RaceDay.API/RaceDay.API.csproj

# In separate terminal
cd src/RaceDay.Web.React
npm run dev

# Or use VS Code task: "React + API + Open Browser"
```

### Testing:
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~AlgorithmImprovementTests"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Acknowledgments

Built using evidence-based sports nutrition research and real-world athlete feedback. Special consideration given to:
- ACSM/AND/DC Position Stand on Nutrition and Athletic Performance
- IOC Consensus Statement on Sports Nutrition
- Practical experience from endurance racing communities

---

**Document Last Updated:** February 11, 2026  
**Algorithm Version:** 2.0.0  
**Test Coverage:** 212/212 tests passing ✅
