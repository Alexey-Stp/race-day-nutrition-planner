# 🌡️ Temperature Algorithm - Visual Guide

## Decision Tree: How Temperature Affects Nutrition

```
START: Athlete wants nutrition plan
    ↓
INPUT: Temperature = ?°C
    ↓
    ├─→ STEP 1: Calculate CARBOHYDRATES
    │   │
    │   ├─ Intensity only (Easy: 50, Moderate: 70, Hard: 90)
    │   ├─ Add +10g/hr if duration > 5 hours (non-easy)
    │   │
    │   └─ TEMPERATURE: ❌ IGNORED
    │       └─ Carbs ALWAYS same regardless of temperature
    │
    ├─→ STEP 2: Calculate FLUIDS
    │   │
    │   ├─ Start: 500 ml/hr (baseline)
    │   │
    │   ├─ Temperature Check:
    │   │   ├─ If temp ≤ 5°C    → -100 ml/hr (COLD PENALTY)
    │   │   ├─ If 5°C < temp < 25°C → 0 ml/hr (NEUTRAL)
    │   │   └─ If temp ≥ 25°C   → +200 ml/hr (HOT BONUS)
    │   │
    │   ├─ Weight Check:
    │   │   ├─ If weight > 80kg → +50 ml/hr
    │   │   └─ If weight < 60kg → -50 ml/hr
    │   │
    │   └─ Safety: Clamp to [300, 900] ml/hr
    │
    └─→ STEP 3: Calculate SODIUM
        │
        ├─ Start: 400 mg/hr (baseline)
        │
        ├─ Temperature Check:
        │   ├─ If temp ≤ 5°C    → 0 mg/hr (COLD: NO EFFECT)
        │   ├─ If 5°C < temp < 25°C → 0 mg/hr (NEUTRAL)
        │   └─ If temp ≥ 25°C   → +200 mg/hr (HOT BONUS)
        │
        ├─ Weight Check:
        │   └─ If weight > 80kg → +100 mg/hr
        │
        └─ Safety: Clamp to [300, 1000] mg/hr

    ↓
OUTPUT: NutritionTargets (carbs, fluids, sodium per hour)
```

---

## Temperature Impact Matrix

```
                COLD (≤5°C)    MODERATE      HOT (≥25°C)
                             (5-25°C)
────────────────────────────────────────────────────────
CARBS           No change   No change      No change
                (50/70/90)  (50/70/90)     (50/70/90)
                                
FLUIDS          -20%        Baseline       +40%
                (400 ml)    (500 ml)       (700 ml)
                                
SODIUM          No change   No change      +50%
                (400 mg)    (400 mg)       (600 mg)
                                
HYDRATION       ↓ Lower     ✓ Normal       ↑↑ Critical
PRIORITY        Risk        Sweat rate

SWEAT LOSS      Low         Moderate       Very high
ELECTROLYTE
LOSS            Low         Moderate       Very high
```

---

## Temperature Zones & Nutrition Adjustments

```
Temperature Scale with Nutrition Adjustments
────────────────────────────────────────────────────────────────

-10°C  0°C   5°C    15°C   25°C   30°C   35°C   40°C
 │      │     │      │      │      │      │      │
 ▼      ▼     ▼      ▼      ▼      ▼      ▼      ▼
 🧊     ❄️   ❄️/COLD 😐    ☀️HOT/⚠️ 🔥HOT  🔥HOT  🔥EXTREME
 
 ─ 100 ml PENALTY ─ ZERO ─── +200 ml BONUS ─ +200 ml BONUS ─

Fluids: 300→400 ml/hr        500 ml/hr        700 ml/hr
Sodium: 400 mg/hr     400→600 mg/hr    600 mg/hr

THRESHOLDS:
│ COLD ≤ 5°C
│ MODERATE 5-25°C (no adjustments)
│ HOT ≥ 25°C
```

---

## Real Race Scenarios

### Scenario 1: Mountain Trail Run - COLD CONDITIONS
```
Conditions: 75 kg runner, 3°C, 2.5 hours, HARD intensity

Temperature: 3°C (≤ 5°C threshold)
           └─ COLD condition

Result:
├─ CARBS:   90 g/hr (no temp effect)
│           └─ Total: 225g
│
├─ FLUIDS: 500 - 100 (cold) - 0 (75kg neutral) = 400 ml/hr
│          └─ Total: 1,000 ml
│
└─ SODIUM: 400 + 0 (cold) + 0 (75kg neutral) = 400 mg/hr
           └─ Total: 1,000 mg

⛰️  Cold conditions = Less fluid (no excessive sweating)
```

---

### Scenario 2: Summer Half Ironman - HOT CONDITIONS
```
Conditions: 75 kg triathlete, 28°C, 4 hours, HARD intensity

Temperature: 28°C (≥ 25°C threshold)
           └─ HOT condition

Result:
├─ CARBS:   90 g/hr (no temp effect)
│           └─ Total: 360g
│
├─ FLUIDS: 500 + 200 (hot) + 0 (75kg neutral) = 700 ml/hr
│          └─ Total: 2,800 ml
│
└─ SODIUM: 400 + 200 (hot) + 0 (75kg neutral) = 600 mg/hr
           └─ Total: 2,400 mg

☀️  Hot conditions = More fluid + more sodium (heavy sweating)
```

---

### Scenario 3: Long Race - EXTREME HEAT + Heavy Athlete
```
Conditions: 85 kg cyclist, 32°C, 5.5 hours, HARD intensity

Temperature: 32°C (≥ 25°C threshold = HOT)
           └─ HOT condition

Result:
├─ CARBS:   90 (base) + 10 (duration > 5hrs) = 100 g/hr
│           └─ Total: 550g (long race bonus!)
│
├─ FLUIDS: 500 + 200 (hot) + 50 (85kg heavy) = 750 ml/hr
│          └─ Total: 4,125 ml (maximum hydration needed!)
│
└─ SODIUM: 400 + 200 (hot) + 100 (85kg heavy) = 700 mg/hr
           └─ Total: 3,850 mg (maximum replacement!)

🔥 Extreme heat + long race + heavy athlete = MAXIMUM needs
```

---

## Code Flow: How Calculations Work

```
FLUIDS CALCULATION
──────────────────

function CalculateFluids(race, athlete):
    fluids = 500  // Start with baseline
    
    // Apply temperature adjustment
    if race.Temperature >= 25°C:
        fluids += 200  // HOT: add bonus
    elif race.Temperature <= 5°C:
        fluids -= 100  // COLD: subtract penalty
    // else: MODERATE, no change
    
    // Apply weight adjustment  
    if athlete.Weight > 80 kg:
        fluids += 50   // Heavy athlete needs more
    elif athlete.Weight < 60 kg:
        fluids -= 50   // Light athlete needs less
    
    // Safety limits
    fluids = Clamp(fluids, 300, 900)  // [min, max]
    
    return fluids


SODIUM CALCULATION
──────────────────

function CalculateSodium(race, athlete):
    sodium = 400  // Start with baseline
    
    // Apply temperature adjustment
    if race.Temperature >= 25°C:
        sodium += 200  // HOT: add bonus
    // COLD: no change (sweat loss minimal)
    // MODERATE: no change
    
    // Apply weight adjustment
    if athlete.Weight > 80 kg:
        sodium += 100  // Heavy athlete needs more
    // else: no penalty (no deduction)
    
    // Safety limits
    sodium = Clamp(sodium, 300, 1000)  // [min, max]
    
    return sodium


CARBOHYDRATES CALCULATION
─────────────────────────

function CalculateCarbohydrates(race):
    carbs = race.Intensity switch:
        Easy     → 50
        Moderate → 70
        Hard     → 90
    
    // Duration bonus (if long race AND not easy)
    if race.Duration > 5 hours AND race.Intensity != Easy:
        carbs += 10
    
    // TEMPERATURE: completely ignored ✗
    
    return carbs
```

---

## Temperature Effects - Summary Pyramid

```
                    PLAN QUALITY
                         ▲
                         │
                         │ 100% Accuracy
                         │ ━━━━━━━━━━━━
                    ┌────────────┐
                    │   RESULT   │◄── Depends on:
                    │  (Targets) │    • Intensity ✓
                    └────────────┘    • Duration ✓
                         ▲            • Weight ✓
                         │            • Temperature ✓
                    ┌─────┴──────┐
                    │ ALGORITHM  │
                    │ Adjustments│
                    └─────┬──────┘
                         │
        ┌────────────┬────┼────┬────────────┐
        │            │    │    │            │
        ▼            ▼    ▼    ▼            ▼
     CARBS     FLUIDS SODIUM WEIGHT    INTENSITY
    (No Temp)  (TEMP!)  (TEMP!)  (Weight) (Duration)
    ────────   ─────────────────  ──────────────────
    Fixed by   Varies with        Adjusts nutrition
    intensity  temperature        based on effort
    & duration
    
    ❌ TEMP    ✅ TEMP     ✅ TEMP  ✅ Effects  ✅ Effects
    ignored    +200 hot    +200 hot included   included
              -100 cold    -100 cold
```

---

## Temperature vs Other Factors

### Which has MORE impact: Temperature or Weight?

```
WEIGHT EFFECT on FLUIDS:
└─ Heavy (>80kg):  +50 ml/hr  (10% increase)
└─ Light (<60kg):  -50 ml/hr  (10% decrease)

HOT TEMPERATURE EFFECT on FLUIDS:
└─ +200 ml/hr  (40% increase) ◄─── MUCH BIGGER!

COLD TEMPERATURE EFFECT on FLUIDS:
└─ -100 ml/hr  (20% decrease) ◄─── MUCH BIGGER!

CONCLUSION: Temperature > Weight
            (Temperature has 2-4x more impact)
```

---

## Decision Guide: What Temp to Use?

```
WEATHER CONDITIONS → TEMPERATURE TO USE
─────────────────────────────────────────

Indoors / Controlled     → 20-22°C (moderate)
Winter race (below 5°)   → Actual temperature (gets -100 ml penalty)
Spring/Fall (5-25°C)     → Use actual temperature (no adjustments)
Summer race (25-35°C)    → Use actual temperature (gets +200 ml, +200 mg bonus)
Desert / Extreme heat    → Use actual temperature (clamped to maximums)

KEY: Using accurate temperature is critical!
     ├─ Too low temp = underestimate hydration needs
     └─ Too high temp = overestimate hydration needs
```

---

## Testing Different Temperatures

### Test Case: SAME athlete, race, intensity, DIFFERENT temperatures

```
ATHLETE: 75 kg, Triathlon, 3.75 hours, HARD intensity

Temperature -10°C  →  Fluids: 400 ml/hr  |  Sodium: 400 mg/hr
Temperature  0°C   →  Fluids: 400 ml/hr  |  Sodium: 400 mg/hr
Temperature  5°C   →  Fluids: 400 ml/hr  |  Sodium: 400 mg/hr
Temperature 15°C   →  Fluids: 500 ml/hr  |  Sodium: 400 mg/hr ← Baseline
Temperature 25°C   →  Fluids: 700 ml/hr  |  Sodium: 600 mg/hr
Temperature 30°C   →  Fluids: 700 ml/hr  |  Sodium: 600 mg/hr
Temperature 35°C   →  Fluids: 700 ml/hr  |  Sodium: 600 mg/hr

Carbs: 90 g/hr across ALL temperatures ✓ (unchanged)
```

---

## Common Misconceptions

```
❌ MYTH 1: "Higher temperature = need more carbs"
✅ TRUTH:  Temperature NEVER affects carbs (only intensity/duration)

❌ MYTH 2: "Cold weather means you need less sodium"
✅ TRUTH:  Cold has NO effect on sodium (stays at baseline)
          Only heat increases sodium needs

❌ MYTH 3: "Maximum fluid intake is always best"
✅ TRUTH:  Capped at 900 ml/hr (gut absorption limit)
          Cold weather: actually need LESS (400 ml/hr)

❌ MYTH 4: "Temperature changes don't matter much"
✅ TRUTH:  Temperature can change fluids by ±40%!
          That's 200+ ml difference per hour!

❌ MYTH 5: "Cold races = no hydration"
✅ TRUTH:  Cold reduces needs BUT still need minimum 300 ml/hr
```

---

## Next Steps

1. **Read:** `API_PLAN_GENERATION_GUIDE.md` for using the API
2. **Test:** Use `QUICK_TEST_COMMANDS.md` with different temperatures
3. **Verify:** Run test comparing 3°C vs 30°C with same athlete/race
4. **Integrate:** Use temperature inputs in your React Web app

