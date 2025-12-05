# 🏃 Plan Generation Quick Reference

## Three Ways to Generate a Plan

### 1. **Brand Filter** (Recommended - Simplest)
```json
{
  "athleteWeightKg": 75,
  "sportType": "Triathlon",
  "durationHours": 3.75,
  "temperatureC": 22,
  "intensity": "Hard",
  "filter": {
    "brand": "SiS",
    "excludeTypes": null
  }
}
```
✅ Clean | ✅ Easy | ✅ All products from brand automatically selected

---

### 2. **Brand + Exclusions** (Selective)
```json
{
  "athleteWeightKg": 75,
  "sportType": "Marathon",
  "durationHours": 2.5,
  "temperatureC": 20,
  "intensity": "Hard",
  "filter": {
    "brand": "Maurten",
    "excludeTypes": ["caffeine", "bar"]
  }
}
```
✅ Flexible | ✅ Exclude what you don't want | ✅ Exclude types: `["gel", "drink", "bar", "caffeine"]`

---

### 3. **All Brands Filtered** (Comprehensive)
```json
{
  "athleteWeightKg": 75,
  "sportType": "Run",
  "durationHours": 2.0,
  "temperatureC": 18,
  "intensity": "Hard",
  "filter": {
    "brand": null,
    "excludeTypes": ["bar"]
  }
}
```
✅ Comprehensive | ✅ Mix all brands | ✅ Use all product types (except bars)

---

### 4. **Explicit Products** (Legacy - Full Control)
```json
{
  "athleteWeightKg": 75,
  "sportType": "Bike",
  "durationHours": 4.0,
  "temperatureC": 25,
  "intensity": "Moderate",
  "products": [
    {
      "name": "SiS GO Gel",
      "productType": "gel",
      "carbsG": 22,
      "sodiumMg": 10,
      "volumeMl": 60
    },
    {
      "name": "SiS Drink",
      "productType": "drink",
      "carbsG": 36,
      "sodiumMg": 300,
      "volumeMl": 500
    }
  ]
}
```
✅ Full control | ❌ Verbose | ❌ Need product details

---

## Sport Types & Intensity Levels

**Sport Types:** `Run` | `Bike` | `Triathlon`

**Intensity Levels:** `Easy` | `Moderate` | `Hard`

---

## Product Types for Exclusion

```json
"excludeTypes": [
  "gel",          // Exclude energy gels
  "drink",        // Exclude sports drinks
  "bar",          // Exclude energy bars/solids
  "caffeine"      // Exclude caffeinated products
]
```

---

## Optional: Custom Interval

```json
{
  "...": "...",
  "intervalMin": 30  // Default is 20 min, set custom pace
}
```

---

## Using GetSummary() Extension

After API returns plan, get shopping list summary:

```csharp
var summary = plan.GetSummary();
```

Returns simplified structure:
- Activity name & duration
- Nutrition targets (per hour)
- Total nutrition (for whole race)
- Shopping list (products + portions)
- Schedule count

---

## Available Brands & Products

| Brand | Products |
|-------|----------|
| **SiS** | 5 products (gels, drinks) |
| **Maurten** | 7 products (gels, drinks, bars, some with caffeine) |

---

## Error Messages Quick Fix

| Message | Fix |
|---------|-----|
| "Either 'products' or 'filter' must be provided" | Choose one method |
| "No products found matching filter" | Adjust brand or excludeTypes |
| "No drink available" | Ensure excludeTypes doesn't exclude drinks |
| "No gel available" | Ensure excludeTypes doesn't exclude gels |
| "Weight must be between 30-200 kg" | Adjust athleteWeightKg |
| "Duration must be between 0.25-24 hours" | Adjust durationHours |

---

## API Endpoint

```
POST http://localhost:5208/api/plan/generate
```

**Response:** Complete `RaceNutritionPlan` with schedule, targets, and shopping list

