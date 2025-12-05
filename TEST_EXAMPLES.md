# 🧪 Test Examples - Plan Generation Features

## Testing with cURL

### Test 1: Simple Brand Filter (SiS)
```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 70,
    "sportType": "Run",
    "durationHours": 2.5,
    "temperatureC": 18,
    "intensity": "Hard",
    "filter": {
      "brand": "SiS",
      "excludeTypes": null
    }
  }'
```

**Expected:** Plan generated with SiS products (gel + drink)

---

### Test 2: Brand with Exclusions (No Caffeine)
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
      "brand": "Maurten",
      "excludeTypes": ["caffeine"]
    }
  }'
```

**Expected:** Plan uses Maurten products without caffeine

---

### Test 3: All Brands (Exclude Bars)
```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 80,
    "sportType": "Bike",
    "durationHours": 4,
    "temperatureC": 20,
    "intensity": "Moderate",
    "filter": {
      "brand": null,
      "excludeTypes": ["bar"]
    }
  }'
```

**Expected:** All products from all brands, no bars

---

### Test 4: Multiple Exclusions
```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 65,
    "sportType": "Run",
    "durationHours": 1.5,
    "temperatureC": 15,
    "intensity": "Hard",
    "filter": {
      "brand": "SiS",
      "excludeTypes": ["caffeine", "bar", "gel"]
    }
  }'
```

**Expected:** Only SiS drinks (no gel, no bars, no caffeine)

---

### Test 5: Custom Interval (30 min)
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
      "brand": "Maurten",
      "excludeTypes": null
    },
    "intervalMin": 30
  }'
```

**Expected:** Schedule with 30-minute intervals instead of default 20

---

### Test 6: Error - No Filter & No Products
```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Triathlon",
    "durationHours": 3.75,
    "temperatureC": 22,
    "intensity": "Hard"
  }'
```

**Expected Response:**
```json
{
  "detail": "Either 'products' or 'filter' must be provided"
}
```

---

### Test 7: Error - Invalid Brand Filter
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
      "brand": "UnknownBrand",
      "excludeTypes": null
    }
  }'
```

**Expected Response:**
```json
{
  "detail": "No products found matching the specified filter"
}
```

---

### Test 8: Backward Compatibility - Explicit Products
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

**Expected:** Plan works exactly as before (backward compatible)

---

## PowerShell Testing

### Function for Testing
```powershell
function Test-PlanGeneration {
    param(
        [string]$Brand = "SiS",
        [array]$ExcludeTypes = @(),
        [double]$Weight = 75,
        [string]$Sport = "Triathlon",
        [double]$Duration = 3.75,
        [int]$Temperature = 22,
        [string]$Intensity = "Hard"
    )

    $body = @{
        athleteWeightKg = $Weight
        sportType = $Sport
        durationHours = $Duration
        temperatureC = $Temperature
        intensity = $Intensity
        filter = @{
            brand = $Brand
            excludeTypes = if ($ExcludeTypes.Count -eq 0) { $null } else { $ExcludeTypes }
        }
    } | ConvertTo-Json

    $response = Invoke-RestMethod `
        -Uri "http://localhost:5208/api/plan/generate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body

    return $response
}

# Usage examples
Test-PlanGeneration -Brand "SiS"
Test-PlanGeneration -Brand "Maurten" -ExcludeTypes @("caffeine")
Test-PlanGeneration -Brand $null -ExcludeTypes @("bar")
```

---

## JavaScript/Node.js Testing

```javascript
async function testPlanGeneration(config) {
  const response = await fetch('http://localhost:5208/api/plan/generate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      athleteWeightKg: config.weight || 75,
      sportType: config.sport || 'Triathlon',
      durationHours: config.duration || 3.75,
      temperatureC: config.temperature || 22,
      intensity: config.intensity || 'Hard',
      filter: {
        brand: config.brand || 'SiS',
        excludeTypes: config.excludeTypes || null
      }
    })
  });

  return response.json();
}

// Test cases
const tests = [
  { name: "SiS Filter", config: { brand: "SiS" } },
  { name: "Maurten No Caffeine", config: { brand: "Maurten", excludeTypes: ["caffeine"] } },
  { name: "All Brands No Bars", config: { brand: null, excludeTypes: ["bar"] } },
  { name: "Custom Interval", config: { brand: "SiS", intervalMin: 30 } }
];

async function runTests() {
  for (const test of tests) {
    console.log(`\n📋 ${test.name}`);
    try {
      const plan = await testPlanGeneration(test.config);
      console.log(`✅ Success - Schedule items: ${plan.schedule.length}`);
      console.log(`   Total carbs: ${plan.totalCarbsG}g, Fluids: ${plan.totalFluidsMl}ml`);
      
      // Test GetSummary (if available in response)
      if (plan.productSummaries) {
        console.log(`   Shopping list items: ${plan.productSummaries.length}`);
      }
    } catch (error) {
      console.log(`❌ Error: ${error.message}`);
    }
  }
}

runTests();
```

---

## Expected Test Results

### Test Result Structure
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
    { "timeMin": 0, "productName": "SiS GO Isotonic Gel", "amountPortions": 1 },
    { "timeMin": 20, "productName": "SiS GO Electrolyte Drink", "amountPortions": 0.5 }
    // ... more items
  ],
  "totalCarbsG": 225,
  "totalFluidsMl": 1500,
  "totalSodiumMg": 1800,
  "productSummaries": [
    { "productName": "SiS GO Isotonic Gel", "totalPortions": 5 },
    { "productName": "SiS GO Electrolyte Drink", "totalPortions": 3 }
  ]
}
```

---

## Verification Checklist

- [ ] Brand filter works (SiS, Maurten)
- [ ] Null brand returns all brands
- [ ] ExcludeTypes removes products correctly
- [ ] Multiple exclusions work together
- [ ] CustomIntervalMin changes schedule spacing
- [ ] Error when no filter + no products
- [ ] Error when filter matches no products
- [ ] Explicit products still work (backward compatibility)
- [ ] ProductSummaries calculated correctly
- [ ] All 63 tests passing

