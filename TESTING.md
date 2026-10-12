# Testing Nutrition Plan API

This document provides instructions for testing the nutrition plan generation API with predefined scenarios. These tools make it easy to generate plans, review them with experts, and iteratively update the algorithm.

## Quick Start

### 1. Start the API Server

```bash
dotnet run --project src/RaceDay.API/RaceDay.API.csproj
```

The API will be available at `http://localhost:5208`

### 2. Choose Your Testing Method

You have three options for testing the API:

#### Option A: Use the Quick Test Script (Easiest)

```bash
# Show available scenarios
./test-nutrition-plan.sh

# Test specific scenario
./test-nutrition-plan.sh half-triathlon

# Test all scenarios
./test-nutrition-plan.sh all
```

#### Option B: Use HTTP File in VS Code (Recommended for Development)

1. Install the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) for VS Code
2. Open `src/RaceDay.API/RaceDay.API.http`
3. Click "Send Request" above any test scenario
4. View the response in a new tab

#### Option C: Use curl Directly (Most Flexible)

```bash
curl -X POST http://localhost:5208/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 90,
    "sportType": "Triathlon",
    "durationHours": 4.5,
    "temperatureC": 20,
    "intensity": "Hard",
    "products": [...]
  }' | python3 -m json.tool
```

## Predefined Test Scenarios

All scenarios use:
- **Athlete Weight**: 90kg
- **Intensity**: Hard (race pace)
- **Temperature**: Moderate (18-22°C)

### 1. Half Triathlon - 4:30 hours

**Scenario**: Half distance triathlon (1.9km swim, 90km bike, 21.1km run)
- Duration: 4.5 hours
- Sport Type: Triathlon
- Products: Maurten GEL 100, Maurten Drink Mix 320

**Test Command**:
```bash
./test-nutrition-plan.sh half-triathlon
```

### 2. Full Triathlon - 10 hours

**Scenario**: Full distance triathlon (3.8km swim, 180km bike, 42.2km run)
- Duration: 10 hours
- Sport Type: Triathlon
- Products: Maurten GEL 100, Maurten Drink Mix 320, Energy Bar

**Test Command**:
```bash
./test-nutrition-plan.sh full-triathlon
```

### 3. Half Marathon - 21km

**Scenario**: 21.1km running race
- Duration: 2 hours
- Sport Type: Run
- Products: SiS GO Isotonic Energy Gel, SiS GO Electrolyte Drink

**Test Command**:
```bash
./test-nutrition-plan.sh half-marathon
```

### 4. Full Marathon - 42km

**Scenario**: 42.2km running race
- Duration: 4 hours
- Sport Type: Run
- Products: Maurten GEL 100, Maurten Drink Mix 320

**Test Command**:
```bash
./test-nutrition-plan.sh full-marathon
```

### 5. Bike Ride - 4 hours

**Scenario**: Long cycling training or race
- Duration: 4 hours
- Sport Type: Bike
- Products: SiS Beta Fuel Gel, SiS GO Electrolyte Drink

**Test Command**:
```bash
./test-nutrition-plan.sh bike-4h
```

## Understanding the Response

The API returns a detailed nutrition plan with the following structure:

```json
{
  "race": {
    "sportType": "Triathlon",
    "durationHours": 4.5,
    "temperature": "Moderate",
    "intensity": "Hard"
  },
  "athlete": {
    "weightKg": 90
  },
  "nutritionSchedule": [
    {
      "timeMin": 243,
      "phase": "Run",
      "phaseDescription": "Run - Stomach more sensitive, prefer gels and drinks",
      "productName": "Maurten GEL 100",
      "amountPortions": 1,
      "action": "Squeeze",
      "totalCarbsSoFar": 25,
      "hasCaffeine": false
    }
    // ... more events
  ],
  "shoppingSummary": {
    "items": [
      {
        "productName": "Maurten GEL 100",
        "totalPortions": 3,
        "totalCarbs": 75
      }
    ],
    "totalProductCount": 3,
    "totalCarbs": 75
  }
}
```

### Key Fields to Review

- **nutritionSchedule**: Time-based plan showing when to consume each product
  - `timeMin`: Time in minutes from race start
  - `phase`: Current race phase (Swim, Bike, Run, or transition)
  - `productName`: Which product to consume
  - `amountPortions`: How much to consume (e.g., 1 gel, 0.5 bottle)
  - `totalCarbsSoFar`: Running total of carbohydrates consumed

- **shoppingSummary**: Total quantities needed for the race
  - Shows how many of each product to bring
  - Useful for race day preparation

## Customizing Test Scenarios

### Modify Script Parameters

Edit `test-nutrition-plan.sh` to change:
- Athlete weight (`athleteWeightKg`)
- Race duration (`durationHours`)
- Temperature (`temperatureC`)
- Intensity level (`Easy`, `Moderate`, `Hard`)
- Products and their nutritional values

### Create Custom Scenarios

Add new scenarios by copying an existing function in the script:

```bash
test_my_custom_race() {
    print_header "Scenario: My Custom Race"
    
    curl -s -X POST "$API_URL" \
      -H "Content-Type: application/json" \
      -d '{
        "athleteWeightKg": 75,
        "sportType": "Run",
        "durationHours": 3,
        "temperatureC": 25,
        "intensity": "Moderate",
        "products": [...]
      }' | python3 -m json.tool
}
```

### Using the HTTP File

The `src/RaceDay.API/RaceDay.API.http` file contains all scenarios and can be easily modified:

1. Open the file in VS Code
2. Edit the JSON request body
3. Click "Send Request" to test
4. Compare results with different parameters

## API Parameters Reference

### Sport Types
- `Run`: Running events
- `Bike`: Cycling events
- `Triathlon`: Multi-sport events (swim/bike/run)

### Intensity Levels
- `Easy`: Recovery pace (50g carbs/hour)
- `Moderate`: Steady pace (70g carbs/hour)
- `Hard`: Race pace (90g carbs/hour)

### Product Types
- `gel`: Energy gels (e.g., Maurten GEL 100)
- `drink`: Sports drinks (e.g., Maurten Drink Mix)
- `bar`: Energy bars

### Temperature Effects
- **Cold** (≤5°C): -100ml/hour fluid adjustment
- **Moderate** (6-24°C): No adjustment
- **Hot** (≥25°C): +200ml/hour fluid, +200mg/hour sodium

### Weight Considerations
- **Heavy** (>80kg): +50ml/hour fluid, +100mg/hour sodium
- **Normal** (60-80kg): Standard values
- **Light** (<60kg): -50ml/hour fluid

## Workflow for Algorithm Updates

1. **Generate Initial Plan**
   ```bash
   ./test-nutrition-plan.sh half-triathlon > plan-v1.json
   ```

2. **Review with Experts**
   - Share the generated plan with nutrition experts
   - Document feedback and recommendations

3. **Update Algorithm**
   - Modify calculation logic in `src/RaceDay.Core/Services/NutritionCalculator.cs`
   - Adjust timing in `src/RaceDay.Core/Services/NutritionPlanService.cs`

4. **Regenerate and Compare**
   ```bash
   ./test-nutrition-plan.sh half-triathlon > plan-v2.json
   diff plan-v1.json plan-v2.json
   ```

5. **Run Tests**
   ```bash
   dotnet test
   ```

6. **Iterate**
   - Repeat steps 1-5 until the plan meets expert approval

## Additional API Endpoints

### Get Available Products
```bash
curl http://localhost:5208/api/products
```

### Get Products by Type
```bash
curl http://localhost:5208/api/products/type/gel
curl http://localhost:5208/api/products/type/drink
```

### Get Predefined Activities
```bash
curl http://localhost:5208/api/activities
```

### Calculate Nutrition Targets Only
```bash
curl -X POST http://localhost:5208/api/metadata/targets \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 90,
    "sportType": "Triathlon",
    "durationHours": 4.5,
    "temperature": "Moderate",
    "intensity": "Hard"
  }'
```

## Troubleshooting

### API Not Running
If you see "Warning: API might not be running", start it with:
```bash
dotnet run --project src/RaceDay.API/RaceDay.API.csproj
```

### Port Already in Use
If port 5208 is busy, kill the existing process or change the port in `appsettings.json`

### Invalid JSON
Ensure all JSON is properly formatted. Use a JSON validator if needed.

### Missing Products
The API requires at least one gel and one drink product. Ensure your products array includes both types.

## Next Steps

1. ✅ Generate plans using predefined scenarios
2. 📋 Review generated plans with experts
3. 🔧 Update algorithm based on feedback
4. 🔁 Regenerate and compare results
5. ✅ Run tests to ensure no regressions
6. 🚀 Deploy updated version

## Support

For questions or issues:
1. Check the main [README.md](README.md) for architecture details
2. Review the API documentation at http://localhost:5208/swagger/ui
3. Examine test cases in `tests/RaceDay.Core.Tests/`
