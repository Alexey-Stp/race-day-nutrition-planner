# 🏃 Race Day Nutrition Planner - Plan Generation Guide

## Overview

The plan generation API has been enhanced to support **flexible product selection**:
1. **Explicit Products** - Specify individual products directly
2. **Brand-based Filtering** - Specify just the brand (e.g., "SiS") and get all products from that brand
3. **Product Type Exclusion** - Exclude specific product types (gel, drink, bar, caffeine)

## Request Format

### POST `/api/plan/generate`

```json
{
  "athleteWeightKg": 75,
  "sportType": "Triathlon",
  "durationHours": 3.75,
  "temperatureC": 22,
  "intensity": "Hard",
  "products": [...],              // OPTIONAL: Explicit products
  "filter": {...},                // OPTIONAL: Product filter (brand + exclusions)
  "intervalMin": 20               // OPTIONAL: Custom interval (default 20 min)
}
```

**Note:** Either `products` or `filter` must be provided (not both)

---

## Example 1: Using Explicit Products (Original Method)

Specify exact products to use in the plan:

```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Triathlon",
    "durationHours": 3.75,
    "temperatureC": 22,
    "intensity": "Hard",
    "products": [
      {
        "name": "SiS GO Isotonic Gel",
        "productType": "gel",
        "carbsG": 22,
        "sodiumMg": 10,
        "volumeMl": 60
      },
      {
        "name": "Maurten Drink Mix 320",
        "productType": "drink",
        "carbsG": 80,
        "sodiumMg": 500,
        "volumeMl": 500
      }
    ]
  }'
```

---

## Example 2: Using Brand Filter (Recommended)

Get all products from a brand - **simpler and cleaner**:

```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Triathlon",
    "durationHours": 3.75,
    "temperatureC": 22,
    "intensity": "Hard",
    "filter": {
      "brand": "SiS",
      "excludeTypes": null
    }
  }'
```

**Response includes all SiS products automatically selected for the race profile**

---

## Example 3: Brand Filter with Exclusions

Use SiS products BUT exclude caffeine and bars:

```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Marathon",
    "durationHours": 3.5,
    "temperatureC": 18,
    "intensity": "Hard",
    "filter": {
      "brand": "SiS",
      "excludeTypes": ["caffeine", "bar"]
    }
  }'
```

**Result:** Only gel and drink products from SiS are available for the plan

---

## Example 4: All Products (No Brand Filter)

Use all products from all brands, but exclude bars:

```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 80,
    "sportType": "Run",
    "durationHours": 2.5,
    "temperatureC": 20,
    "intensity": "Hard",
    "filter": {
      "brand": null,
      "excludeTypes": ["bar"]
    }
  }'
```

**Result:** All gel and drink products from all brands

---

## Example 5: Custom Intake Interval

Generate a plan with 30-minute intake intervals (instead of default 20):

```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 70,
    "sportType": "Bike",
    "durationHours": 4,
    "temperatureC": 25,
    "intensity": "Moderate",
    "filter": {
      "brand": "Maurten",
      "excludeTypes": null
    },
    "intervalMin": 30
  }'
```

**Result:** Nutrition intakes every 30 minutes instead of 20

---

## Available Product Types for Exclusion

| Type | Description | Examples |
|------|-------------|----------|
| `gel` | Energy gels | SiS GO Gel, Maurten Gel 100 |
| `drink` | Sports drinks | SiS Electrolyte Drink, Maurten Drink Mix |
| `bar` | Energy bars/solids | Maurten Solids Bar |
| `caffeine` | Products with caffeine | SiS GO Caffeine Gel, Maurten Gel Caf 100 |

---

## Available Brands

- `SiS` (5 products)
- `Maurten` (7 products)

Leave `brand` as `null` to use all brands.

---

## Response Format

All plan generation requests return the same structure:

```json
{
  "race": {
    "sportType": "Triathlon",
    "durationHours": 3.75,
    "temperatureC": 22,
    "intensity": "Hard"
  },
  "targets": {
    "carbsGPerHour": 75,
    "fluidsMlPerHour": 500,
    "sodiumMgPerHour": 600
  },
  "schedule": [
    {
      "timeMin": 0,
      "productName": "SiS GO Isotonic Gel",
      "amountPortions": 1
    },
    {
      "timeMin": 20,
      "productName": "Maurten Drink Mix 320",
      "amountPortions": 0.5
    }
  ],
  "totalCarbsG": 225,
  "totalFluidsMl": 1500,
  "totalSodiumMg": 1800,
  "productSummaries": [
    {
      "productName": "SiS GO Isotonic Gel",
      "totalPortions": 5
    },
    {
      "productName": "Maurten Drink Mix 320",
      "totalPortions": 3
    }
  ]
}
```

---

## Using Plan Summary (Extension Method)

Extract a simplified shopping list and nutrition summary:

```csharp
// In C# code, after getting the plan response:
var planSummary = plan.GetSummary();

// Returns:
// {
//   "activityName": "Triathlon",
//   "durationHours": 3.75,
//   "temperatureC": 22,
//   "intensityLevel": "Hard",
//   "nutritionTargets": {
//     "carbsGPerHour": 75,
//     "fluidsMlPerHour": 500,
//     "sodiumMgPerHour": 600
//   },
//   "totalNutrition": {
//     "carbsG": 225,
//     "fluidsMl": 1500,
//     "sodiumMg": 1800
//   },
//   "shoppingList": [
//     { "productName": "SiS GO Isotonic Gel", "totalPortions": 5 },
//     { "productName": "Maurten Drink Mix 320", "totalPortions": 3 }
//   ],
//   "scheduleCount": 11
// }
```

---

## Error Handling

| Error | Cause | Solution |
|-------|-------|----------|
| `400 Bad Request` | Both `products` and `filter` provided | Provide only one |
| `400 Bad Request` | Neither `products` nor `filter` provided | Specify one method |
| `400 Bad Request` | No products found matching filter | Adjust filter criteria |
| `400 Bad Request` | Invalid sport type / intensity | Use valid enum values |
| `400 Bad Request` | Weight outside valid range (30-200 kg) | Adjust athlete weight |
| `400 Bad Request` | Duration outside valid range (0.25-24 hours) | Adjust race duration |
| `400 Bad Request` | No drink available | Filter includes at least one drink |
| `400 Bad Request` | No gel available | Filter includes at least one gel |

---

## Best Practices

### 1. **For Quick Testing** - Use Brand Filter
```json
{
  "filter": { "brand": "SiS" }
}
```
Clean, simple, no need to look up product details.

### 2. **For Specific Requirements** - Use Exclusions
```json
{
  "filter": {
    "brand": "Maurten",
    "excludeTypes": ["caffeine"]
  }
}
```
Avoid caffeine products but keep all others.

### 3. **For Full Control** - Use Explicit Products
```json
{
  "products": [...]
}
```
When you need exact product specifications.

### 4. **For Different Pace** - Use Custom Intervals
```json
{
  "intervalMin": 30
}
```
Longer intervals for steady efforts, shorter for intensity.

---

## Integration Tips

### JavaScript/Fetch Example
```javascript
async function generatePlan(athleteWeight, sport, duration, brand, excludeTypes) {
  const response = await fetch('http://localhost:5208/api/plan/generate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      athleteWeightKg: athleteWeight,
      sportType: sport,
      durationHours: duration,
      temperatureC: 20,
      intensity: 'Hard',
      filter: {
        brand: brand,
        excludeTypes: excludeTypes || []
      }
    })
  });
  
  const plan = await response.json();
  const summary = plan.GetSummary?.() || plan;
  
  return summary;
}

// Usage
const plan = await generatePlan(75, 'Triathlon', 3.75, 'SiS', ['caffeine']);
```

### React Hook Example
```javascript
const [plan, setPlan] = useState(null);

const generateRacePlan = async (filter) => {
  try {
    const response = await fetch('http://localhost:5208/api/plan/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        athleteWeightKg: 75,
        sportType: 'Triathlon',
        durationHours: 3.75,
        temperatureC: 22,
        intensity: 'Hard',
        filter
      })
    });
    
    setPlan(await response.json());
  } catch (error) {
    console.error('Plan generation failed:', error);
  }
};
```

