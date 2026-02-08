# Example Nutrition Plan Outputs

This directory is for storing example nutrition plan outputs that can be reviewed with experts.

## Generating Examples

To save a plan output for review:

```bash
# Start the API server
dotnet run --project src/RaceDay.API/RaceDay.API.csproj

# Generate and save a plan
./test-nutrition-plan.sh half-triathlon > example-outputs/half-triathlon-v1.json
```

## Comparing Versions

After updating the algorithm, you can compare different versions:

```bash
# Generate version 2 after algorithm changes
./test-nutrition-plan.sh half-triathlon > example-outputs/half-triathlon-v2.json

# Compare the two versions
diff example-outputs/half-triathlon-v1.json example-outputs/half-triathlon-v2.json
```

## Example Scenarios

Common scenarios to save for expert review:

1. **Half Triathlon** (4:30 hours, 90kg, Race Intensity)
   ```bash
   ./test-nutrition-plan.sh half-triathlon > example-outputs/half-triathlon.json
   ```

2. **Full Triathlon** (10 hours, 90kg, Race Intensity)
   ```bash
   ./test-nutrition-plan.sh full-triathlon > example-outputs/full-triathlon.json
   ```

3. **Half Marathon** (2 hours, 90kg, Race Intensity)
   ```bash
   ./test-nutrition-plan.sh half-marathon > example-outputs/half-marathon.json
   ```

4. **Full Marathon** (4 hours, 90kg, Race Intensity)
   ```bash
   ./test-nutrition-plan.sh full-marathon > example-outputs/full-marathon.json
   ```

5. **Bike Ride** (4 hours, 90kg, Race Intensity)
   ```bash
   ./test-nutrition-plan.sh bike-4h > example-outputs/bike-4h.json
   ```

## Sharing with Experts

Share these JSON files with nutrition experts by:

1. Email the JSON files
2. Share via cloud storage (Google Drive, Dropbox, etc.)
3. Use a JSON formatter online for better readability (e.g., jsonformatter.org)
4. Convert to a more readable format if needed

## What to Review

Key aspects for experts to evaluate:

1. **Nutrition Targets** (carbs/fluids/sodium per hour)
2. **Timing of nutrition events** (when to consume products)
3. **Product quantities** (number of gels, drinks, etc.)
4. **Phase-specific recommendations** (swim/bike/run adjustments)
5. **Shopping summary** (total products needed)
