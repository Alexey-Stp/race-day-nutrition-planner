# 🌡️ Temperature Impact on Nutrition Algorithm

## Overview

Temperature significantly affects **fluid and sodium intake** in the nutrition plan. Carbohydrates are NOT affected by temperature - they're based only on intensity and race duration.

---

## Temperature Thresholds

```
Temperature Ranges
─────────────────────────────────────────

❄️  Very Cold       ≤ 5°C    → Cold Penalty Applied
🥶  Cold            6-15°C   → Baseline
😐  Moderate       15-24°C   → Baseline  
☀️  Warm            25-35°C  → Hot Bonus Applied
🔥  Very Hot        > 35°C   → Hot Bonus Applied

Thresholds:
  Cold threshold:  5°C
  Hot threshold:   25°C
```

---

## Impact on Each Nutrient

### 1️⃣ **Carbohydrates** 🚫
**NO temperature impact**

- Base: 50g/hr (Easy) → 70g/hr (Moderate) → 90g/hr (Hard)
- Duration bonus: +10g/hr for races > 5 hours (non-easy)
- Temperature: **Ignored**

```
Temperature change: No effect on carbs
```

---

### 2️⃣ **Fluids** 💧 ✅ AFFECTED

**Base:** 500 ml/hour

#### Hot Weather (≥ 25°C)
```
Fluids = 500 ml/hr + 200 ml/hr = 700 ml/hr

Why: Increased sweating, greater dehydration risk
Effect: +40% more fluid needed
```

#### Cold Weather (≤ 5°C)
```
Fluids = 500 ml/hr - 100 ml/hr = 400 ml/hr

Why: Reduced sweating, lower dehydration risk
Effect: -20% less fluid needed
```

#### Safe Limits
```
Minimum: 300 ml/hr  (never go below)
Maximum: 900 ml/hr  (never go above)
```

---

### 3️⃣ **Sodium** 🧂 ✅ AFFECTED

**Base:** 400 mg/hour

#### Hot Weather (≥ 25°C)
```
Sodium = 400 mg/hr + 200 mg/hr = 600 mg/hr

Why: Increased sweat loss increases electrolyte loss
Effect: +50% more sodium needed
```

#### Cold Weather (≤ 5°C)
```
Sodium = 400 mg/hr  (NO CHANGE)

Why: Cold weather doesn't significantly increase sweat loss
Effect: Stays at baseline
```

#### Safe Limits
```
Minimum: 300 mg/hr
Maximum: 1000 mg/hr
```

---

## Complete Algorithm

### Step 1: Calculate Carbohydrates (Ignores Temperature)
```
Base = Intensity Level
├─ Easy        → 50 g/hr
├─ Moderate    → 70 g/hr
└─ Hard        → 90 g/hr

If duration > 5 hours AND intensity ≠ Easy:
  Add 10 g/hr bonus

Final Carbs = [Base] + [Bonus if applicable]
```

### Step 2: Calculate Fluids (Temperature Dependent)
```
Start = 500 ml/hr

Temperature Adjustments:
├─ If temp ≥ 25°C  → +200 ml/hr (hot weather)
└─ If temp ≤ 5°C   → -100 ml/hr (cold weather)

Weight Adjustments:
├─ If weight > 80kg  → +50 ml/hr (heavier athletes)
└─ If weight < 60kg  → -50 ml/hr (lighter athletes)

Final Fluids = Math.Clamp(result, 300, 900 ml/hr)
```

### Step 3: Calculate Sodium (Temperature Dependent)
```
Start = 400 mg/hr

Temperature Adjustments:
├─ If temp ≥ 25°C  → +200 mg/hr (hot weather)
└─ If temp ≤ 5°C   → NO CHANGE  (cold weather ignored)

Weight Adjustments:
├─ If weight > 80kg  → +100 mg/hr (heavier athletes)
└─ If weight < 60kg  → NO CHANGE  (not penalized)

Final Sodium = Math.Clamp(result, 300, 1000 mg/hr)
```

---

## Real-World Examples

### Example 1: 75 kg Athlete, Triathlon, 3.75 hours, WARM (22°C), HARD

```
📊 CARBOHYDRATES (No temperature effect)
─────────────────────────────────────
Base:         90 g/hr (Hard intensity)
Duration:     N/A (3.75 hrs < 5 hrs)
Final Carbs:  90 g/hr

Total for race:  90 × 3.75 = 337.5g

💧 FLUIDS (Temperature effect +0% at 22°C)
─────────────────────────────────────────
Base:              500 ml/hr
Temperature:       0 ml/hr (22°C is between cold & hot)
Weight (75kg):     0 ml/hr (75 is between thresholds)
Final Fluids:      500 ml/hr

Total for race:  500 × 3.75 = 1,875 ml

🧂 SODIUM (Temperature effect +0% at 22°C)
─────────────────────────────────────────
Base:              400 mg/hr
Temperature:       0 mg/hr (22°C is between cold & hot)
Weight (75kg):     0 mg/hr (75 is between thresholds)
Final Sodium:      400 mg/hr

Total for race:  400 × 3.75 = 1,500 mg
```

---

### Example 2: 75 kg Athlete, Marathon, 2.5 hours, HOT (30°C), HARD

```
📊 CARBOHYDRATES (No temperature effect)
─────────────────────────────────────
Base:         90 g/hr (Hard intensity)
Duration:     N/A (2.5 hrs < 5 hrs)
Final Carbs:  90 g/hr

Total for race:  90 × 2.5 = 225g  ✅ SAME as warm weather

💧 FLUIDS (Temperature effect +200 ml/hr at 30°C)
─────────────────────────────────────────────────
Base:              500 ml/hr
Temperature:      +200 ml/hr (30°C ≥ 25°C threshold)
Weight (75kg):       0 ml/hr
Final Fluids:      700 ml/hr  ⬆️ 40% INCREASE

Total for race:  700 × 2.5 = 1,750 ml

🧂 SODIUM (Temperature effect +200 mg/hr at 30°C)
────────────────────────────────────────────────
Base:              400 mg/hr
Temperature:      +200 mg/hr (30°C ≥ 25°C threshold)
Weight (75kg):       0 mg/hr
Final Sodium:      600 mg/hr  ⬆️ 50% INCREASE

Total for race:  600 × 2.5 = 1,500 mg
```

**Difference:** Hot weather = MORE fluids + MORE sodium, SAME carbs

---

### Example 3: 75 kg Athlete, Run, 1.5 hours, COLD (3°C), HARD

```
📊 CARBOHYDRATES (No temperature effect)
─────────────────────────────────────
Base:         90 g/hr (Hard intensity)
Duration:     N/A (1.5 hrs < 5 hrs)
Final Carbs:  90 g/hr

Total for race:  90 × 1.5 = 135g  ✅ SAME as warm weather

💧 FLUIDS (Temperature effect -100 ml/hr at 3°C)
─────────────────────────────────────────────────
Base:              500 ml/hr
Temperature:      -100 ml/hr (3°C ≤ 5°C threshold)
Weight (75kg):       0 ml/hr
Final Fluids:      400 ml/hr  ⬇️ 20% DECREASE

Total for race:  400 × 1.5 = 600 ml

🧂 SODIUM (Temperature NO effect at 3°C)
──────────────────────────────────────
Base:              400 mg/hr
Temperature:       0 mg/hr (cold doesn't increase sweat loss)
Weight (75kg):      0 mg/hr
Final Sodium:      400 mg/hr  ⚪ NO CHANGE

Total for race:  400 × 1.5 = 600 mg
```

**Difference:** Cold weather = LESS fluids, SAME sodium + carbs

---

### Example 4: 85 kg (Heavy) Athlete, Triathlon, 5.5 hours, HOT (28°C), HARD

```
📊 CARBOHYDRATES (No temperature effect)
─────────────────────────────────────
Base:         90 g/hr (Hard intensity)
Duration:     +10 g/hr (5.5 hrs > 5 hrs AND hard intensity)
Final Carbs:  100 g/hr

Total for race:  100 × 5.5 = 550g  ⬆️ BONUS from duration

💧 FLUIDS (Temperature + Weight effects)
──────────────────────────────────────
Base:              500 ml/hr
Temperature:      +200 ml/hr (28°C ≥ 25°C)
Weight (85kg):     +50 ml/hr  (85kg > 80kg)
Final Fluids:      750 ml/hr  ⬆️ 50% INCREASE

Total for race:  750 × 5.5 = 4,125 ml

🧂 SODIUM (Temperature + Weight effects)
────────────────────────────────────────
Base:              400 mg/hr
Temperature:      +200 mg/hr (28°C ≥ 25°C)
Weight (85kg):     +100 mg/hr (85kg > 80kg)
Final Sodium:      700 mg/hr  ⬆️ 75% INCREASE

Total for race:  700 × 5.5 = 3,850 mg
```

**Combined Effects:** Large athlete + hot weather + long race = maximum needs

---

### Example 5: 55 kg (Light) Athlete, 5K Run, 0.25 hours, COLD (4°C), EASY

```
📊 CARBOHYDRATES (No temperature effect)
─────────────────────────────────────
Base:         50 g/hr (Easy intensity)
Duration:     N/A (easy = no bonus)
Final Carbs:  50 g/hr

Total for race:  50 × 0.25 = 12.5g  ✅ MINIMAL

💧 FLUIDS (Temperature + Weight effects)
──────────────────────────────────────
Base:              500 ml/hr
Temperature:      -100 ml/hr (4°C ≤ 5°C)
Weight (55kg):     -50 ml/hr  (55kg < 60kg)
Final Fluids:      350 ml/hr  ⬇️ 30% DECREASE

Clamped to:        300 ml/hr (minimum safety limit)

Total for race:  300 × 0.25 = 75 ml

🧂 SODIUM (Temperature + Weight effects)
────────────────────────────────────────
Base:              400 mg/hr
Temperature:       0 mg/hr (cold weather ignored)
Weight (55kg):      0 mg/hr (no penalty for light athletes)
Final Sodium:      400 mg/hr  ✅ NO CHANGE

Total for race:  400 × 0.25 = 100 mg
```

**Minimalist approach:** Light athlete + cold + short race = very conservative

---

## Summary Table: Temperature Effects

```
╔════════════════════╦════════════╦═══════════╦══════════╗
║ Metric             ║ Cold (≤5°C)║ Moderate  ║ Hot (≥25°)║
║                    ║            ║(5-25°C)   ║          ║
╠════════════════════╬════════════╬═══════════╬══════════╣
║ CARBOHYDRATES      ║ No change  ║ No change ║ No change║
║ (depends on        ║            ║           ║          ║
║  intensity only)   ║            ║           ║          ║
╠════════════════════╬════════════╬═══════════╬══════════╣
║ FLUIDS             ║ -100 ml/hr ║ Baseline  ║ +200 ml/hr║
║ (500 ml baseline)  ║ = 400 ml   ║ 500 ml    ║ = 700 ml  ║
║                    ║ (-20%)     ║           ║ (+40%)    ║
╠════════════════════╬════════════╬═══════════╬══════════╣
║ SODIUM             ║ No change  ║ No change ║ +200 mg/hr║
║ (400 mg baseline)  ║ 400 mg     ║ 400 mg    ║ = 600 mg  ║
║                    ║            ║           ║ (+50%)    ║
╚════════════════════╩════════════╩═══════════╩══════════╝
```

---

## Why These Rules?

### 🌡️ Hot Weather (≥ 25°C)

**Fluid Increase:**
- Increased sweating due to high temperature
- Body needs more cooling through sweat evaporation
- Greater dehydration risk
- **Result:** +200 ml/hr fluid

**Sodium Increase:**
- Sweat contains sodium (salts)
- More sweat = more electrolyte loss
- Sodium replacement prevents cramping and hyponatremia
- **Result:** +200 mg/hr sodium

**Carbs Unchanged:**
- Temperature doesn't affect energy needs
- Only intensity and duration matter

---

### ❄️ Cold Weather (≤ 5°C)

**Fluid Decrease:**
- Reduced sweating due to low temperature
- Body retains more heat
- Lower dehydration risk
- Excessive fluid can cause discomfort (stomach sloshing)
- **Result:** -100 ml/hr fluid

**Sodium Unchanged:**
- Cold weather doesn't significantly increase sweat loss
- Electrolyte loss remains minimal
- Body maintains baseline sodium needs
- **Result:** No change

**Carbs Unchanged:**
- Temperature doesn't affect energy needs

---

## Safe Limits (Clamping)

The algorithm uses **safety boundaries** to prevent extremes:

```
FLUIDS:
├─ Minimum: 300 ml/hr  (absolute safety floor)
├─ Maximum: 900 ml/hr  (gut absorption limit)
└─ Reason: Prevent both dehydration and hyponatremia

SODIUM:
├─ Minimum: 300 mg/hr
├─ Maximum: 1000 mg/hr
└─ Reason: Prevent electrolyte imbalances
```

Example:
```
Cold (4°C) + Light athlete (55kg):
  Raw calculation: 500 - 100 - 50 = 350 ml/hr
  After clamping: 300 ml/hr (minimum enforced)
```

---

## API Usage - Temperature Impact

### Example 1: Test Temperature Effect (SAME race, different temps)

```bash
# COLD weather (3°C)
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Triathlon",
    "durationHours": 3.75,
    "temperatureC": 3,
    "intensity": "Hard",
    "filter": {"brand": "SiS"}
  }'

# Response will show: 
#   targets.fluidsMlPerHour: 400 ml
#   targets.sodiumMgPerHour: 400 mg

---

# HOT weather (30°C) - SAME other parameters
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Triathlon",
    "durationHours": 3.75,
    "temperatureC": 30,
    "intensity": "Hard",
    "filter": {"brand": "SiS"}
  }'

# Response will show:
#   targets.fluidsMlPerHour: 700 ml  (+75% more!)
#   targets.sodiumMgPerHour: 600 mg  (+50% more!)
#   targets.carbsGPerHour: 90 g      (SAME - unchanged)
```

---

## Key Takeaways

| Point | Details |
|-------|---------|
| **Carbs Immune** | Temperature NEVER affects carbohydrate targets |
| **Hot = More Fluid** | +200 ml/hr (requires more hydration) |
| **Hot = More Sodium** | +200 mg/hr (replaces sweat electrolytes) |
| **Cold = Less Fluid** | -100 ml/hr (reduces dehydration risk) |
| **Cold = Same Sodium** | No change (sweat loss minimal) |
| **Safe Limits** | Fluids 300-900 ml, Sodium 300-1000 mg |
| **Combined Effects** | Temperature + weight + intensity all interact |

---

## Testing Temperature Effects

Use `QUICK_TEST_COMMANDS.md` to test with different temperatures:

```powershell
# Function to test temperature impact
function Test-TemperatureEffect {
    param([int]$Temperature)
    
    $body = @{
        athleteWeightKg = 75
        sportType = "Triathlon"
        durationHours = 3.75
        temperatureC = $Temperature
        intensity = "Hard"
        filter = @{brand = "SiS"}
    } | ConvertTo-Json

    $response = Invoke-RestMethod `
        -Uri "http://localhost:5208/api/plan/generate" `
        -Method Post -ContentType "application/json" `
        -Body $body

    Write-Host "Temp: $Temperature°C" -ForegroundColor Cyan
    Write-Host "  Fluids: $($response.targets.fluidsMlPerHour) ml/hr" -ForegroundColor Yellow
    Write-Host "  Sodium: $($response.targets.sodiumMgPerHour) mg/hr" -ForegroundColor Yellow
    Write-Host "  Carbs:  $($response.targets.carbsGPerHour) g/hr" -ForegroundColor Green
}

# Test across temperature range
-10, 0, 5, 15, 25, 30, 35 | ForEach-Object { Test-TemperatureEffect $_ }
```

