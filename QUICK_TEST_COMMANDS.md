# 🧪 Quick Testing Commands

## Start the API

```bash
cd C:\Users\2ndst\source\repos\RaceDayNutritionPlanner
dotnet run --project src/RaceDay.API
```

Then in another terminal, try these commands:

---

## Test 1: Basic Brand Filter (SiS)

```bash
curl -X POST http://localhost:5208/api/plan/generate ^
  -H "Content-Type: application/json" ^
  -d "{\"athleteWeightKg\":75,\"sportType\":\"Triathlon\",\"durationHours\":3.75,\"temperatureC\":22,\"intensity\":\"Hard\",\"filter\":{\"brand\":\"SiS\",\"excludeTypes\":null}}"
```

**Expected:** Plan with SiS products (gel + drink)

---

## Test 2: Brand + Exclusions (Maurten, No Caffeine)

```bash
curl -X POST http://localhost:5208/api/plan/generate ^
  -H "Content-Type: application/json" ^
  -d "{\"athleteWeightKg\":75,\"sportType\":\"Marathon\",\"durationHours\":2.5,\"temperatureC\":18,\"intensity\":\"Hard\",\"filter\":{\"brand\":\"Maurten\",\"excludeTypes\":[\"caffeine\"]}}"
```

**Expected:** Maurten products without caffeine variants

---

## Test 3: All Brands, No Bars

```bash
curl -X POST http://localhost:5208/api/plan/generate ^
  -H "Content-Type: application/json" ^
  -d "{\"athleteWeightKg\":80,\"sportType\":\"Bike\",\"durationHours\":4,\"temperatureC\":20,\"intensity\":\"Moderate\",\"filter\":{\"brand\":null,\"excludeTypes\":[\"bar\"]}}"
```

**Expected:** Products from all brands except bars

---

## Test 4: Custom Interval (30 min)

```bash
curl -X POST http://localhost:5208/api/plan/generate ^
  -H "Content-Type: application/json" ^
  -d "{\"athleteWeightKg\":75,\"sportType\":\"Triathlon\",\"durationHours\":3.75,\"temperatureC\":22,\"intensity\":\"Hard\",\"filter\":{\"brand\":\"SiS\",\"excludeTypes\":null},\"intervalMin\":30}"
```

**Expected:** Schedule with 30-minute intervals instead of 20

---

## Test 5: Old Way - Explicit Products (Backward Compatibility)

```bash
curl -X POST http://localhost:5208/api/plan/generate ^
  -H "Content-Type: application/json" ^
  -d "{\"athleteWeightKg\":75,\"sportType\":\"Triathlon\",\"durationHours\":3.75,\"temperatureC\":22,\"intensity\":\"Hard\",\"products\":[{\"name\":\"SiS GO Gel\",\"productType\":\"gel\",\"carbsG\":22,\"sodiumMg\":10,\"volumeMl\":60},{\"name\":\"Maurten Drink Mix 320\",\"productType\":\"drink\",\"carbsG\":80,\"sodiumMg\":500,\"volumeMl\":500}]}"
```

**Expected:** Plan works exactly as before (backward compatible)

---

## Test 6: Error - No Filter and No Products

```bash
curl -X POST http://localhost:5208/api/plan/generate ^
  -H "Content-Type: application/json" ^
  -d "{\"athleteWeightKg\":75,\"sportType\":\"Triathlon\",\"durationHours\":3.75,\"temperatureC\":22,\"intensity\":\"Hard\"}"
```

**Expected Response:**
```
Either 'products' or 'filter' must be provided
```

---

## Test 7: Error - Invalid Brand

```bash
curl -X POST http://localhost:5208/api/plan/generate ^
  -H "Content-Type: application/json" ^
  -d "{\"athleteWeightKg\":75,\"sportType\":\"Triathlon\",\"durationHours\":3.75,\"temperatureC\":22,\"intensity\":\"Hard\",\"filter\":{\"brand\":\"UnknownBrand\",\"excludeTypes\":null}}"
```

**Expected Response:**
```
No products found matching the specified filter
```

---

## Build & Test (All Projects)

```bash
cd C:\Users\2ndst\source\repos\RaceDayNutritionPlanner

# Build
dotnet build

# Run tests
dotnet test
```

**Expected:** 
- Build: Success (6.3s)
- Tests: 63 passing, 0 failing

---

## Using Swagger UI

After starting the API, open in browser:

```
http://localhost:5208/swagger/ui
```

Then:
1. Expand **"Nutrition Plan"** section
2. Click **POST /api/plan/generate**
3. Click **"Try it out"**
4. Paste request JSON in request body
5. Click **"Execute"**

---

## PowerShell Function for Testing

Save as `test-plan.ps1`:

```powershell
function New-PlanTest {
    param(
        [string]$Brand = "SiS",
        [array]$ExcludeTypes = @(),
        [double]$Weight = 75,
        [string]$Sport = "Triathlon",
        [double]$Duration = 3.75
    )

    $body = @{
        athleteWeightKg = $Weight
        sportType = $Sport
        durationHours = $Duration
        temperatureC = 22
        intensity = "Hard"
        filter = @{
            brand = $Brand
            excludeTypes = if ($ExcludeTypes.Count -eq 0) { $null } else { $ExcludeTypes }
        }
    } | ConvertTo-Json -Depth 10

    Write-Host "Testing: $Sport, $Duration hours, $Weight kg, Brand: $Brand" -ForegroundColor Cyan
    Write-Host "Excluded types: $($ExcludeTypes -join ', ')" -ForegroundColor Cyan

    $response = Invoke-RestMethod `
        -Uri "http://localhost:5208/api/plan/generate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body

    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Schedule items: $($response.schedule.Count)" -ForegroundColor Yellow
    Write-Host "Total carbs: $($response.totalCarbsG)g" -ForegroundColor Yellow
    Write-Host "Total fluids: $($response.totalFluidsMl)ml" -ForegroundColor Yellow
    Write-Host "Shopping list: $($response.productSummaries.Count) items" -ForegroundColor Yellow
    Write-Host ""
    
    return $response
}

# Run tests
New-PlanTest -Brand "SiS"
New-PlanTest -Brand "Maurten" -ExcludeTypes @("caffeine")
New-PlanTest -Brand $null -ExcludeTypes @("bar")
```

Usage:
```powershell
. .\test-plan.ps1
New-PlanTest -Brand "SiS"
```

---

## Verify Implementation

```bash
# Check models were added
grep -r "ProductFilter" src/RaceDay.Core/

# Check filtering method
grep -r "GetFilteredProductsAsync" src/RaceDay.Core/

# Check extensions
cat src/RaceDay.Core/PlanExtensions.cs

# Check API changes
grep -r "GetFilteredProductsAsync" src/RaceDay.API/

# Verify tests
dotnet test -v normal
```

---

## Performance Test

Generate 100 plans quickly:

```powershell
$requests = 1..100 | ForEach-Object {
    @{
        athleteWeightKg = 70 + $_
        sportType = ("Triathlon","Marathon","Bike","Run" | Get-Random)
        durationHours = 2 + (Get-Random -Maximum 4)
        temperatureC = 15 + (Get-Random -Maximum 15)
        intensity = ("Easy","Moderate","Hard" | Get-Random)
        filter = @{
            brand = ("SiS","Maurten",$null | Get-Random)
            excludeTypes = $null
        }
    }
}

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$results = $requests | ForEach-Object {
    Invoke-RestMethod -Uri "http://localhost:5208/api/plan/generate" `
        -Method Post -ContentType "application/json" `
        -Body ($_ | ConvertTo-Json -Depth 10) -ErrorAction SilentlyContinue
}
$stopwatch.Stop()

Write-Host "Generated $($results.Count) plans in $($stopwatch.ElapsedMilliseconds)ms"
Write-Host "Average: $($stopwatch.ElapsedMilliseconds / $results.Count)ms per plan"
```

---

## Response Format Example

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
      "productName": "SiS GO Electrolyte Drink",
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
      "productName": "SiS GO Electrolyte Drink",
      "totalPortions": 3
    }
  ]
}
```

---

## All Tests Should Pass

```
✅ NutritionCalculator: 14 tests
✅ PlanGenerator: 6 tests
✅ Validation: 22 tests
✅ ActivityRepository: 17 tests
✅ PlanExtensions: 4 tests
────────────────────────
✅ TOTAL: 63 tests passing
```

