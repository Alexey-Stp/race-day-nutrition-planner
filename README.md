# Race Day Nutrition Planner

A .NET console application that generates personalized nutrition plans for endurance athletes during races and training sessions.

## Overview

This tool calculates optimal carbohydrate, fluid, and sodium intake recommendations based on:
- Athlete characteristics (weight)
- Race parameters (sport type, duration, temperature, intensity)
- Available nutrition products (gels, drinks, bars)

The application generates a time-based schedule showing when and how much of each product to consume during the race.

## Features

- **Personalized Calculations**: Adjusts nutrition targets based on athlete weight, race intensity, duration, and temperature
- **Flexible Product Support**: Works with various nutrition products (gels, drinks, bars)
- **Time-Based Schedule**: Generates a minute-by-minute nutrition intake plan
- **Smart Recommendations**: 
  - Increases carb intake for harder efforts and longer durations (5+ hours)
  - Adjusts fluid needs based on temperature and athlete weight
  - Optimizes sodium intake for hot conditions and heavier athletes

## Project Structure

```
RaceDayNutritionPlanner/
├── src/
│   ├── RaceDay.Core/          # Core logic library
│   │   ├── Models.cs          # Data models and records
│   │   ├── NutritionCalculator.cs  # Target calculation logic
│   │   └── PlanGenerator.cs   # Schedule generation logic
│   └── RaceDay.CLI/           # Console application
│       └── Program.cs         # Entry point and example usage
└── RaceDayNutritionPlanner.sln
```

## Requirements

- .NET 9.0 SDK or later

## Building the Project

```powershell
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the CLI application
dotnet run --project src/RaceDay.CLI/RaceDay.CLI.csproj
```

## Usage Example

```csharp
using RaceDay.Core;

// Define athlete profile
var athlete = new AthleteProfile(WeightKg: 89);

// Define race profile
var race = new RaceProfile(
    SportType.Triathlon,
    DurationHours: 4.5,
    TemperatureC: 20,
    Intensity: IntensityLevel.Moderate
);

// Define available products
var products = new List<Product>
{
    new Product("Maurten Gel", "gel", CarbsG: 25, SodiumMg: 100),
    new Product("Isotonic Drink 500ml", "drink", CarbsG: 30, SodiumMg: 300, VolumeMl: 500)
};

// Generate nutrition plan
var plan = PlanGenerator.Generate(race, athlete, products);

// Display results
Console.WriteLine($"Carbs/h:  {plan.Targets.CarbsGPerHour} g");
Console.WriteLine($"Fluids/h: {plan.Targets.FluidsMlPerHour} ml");
Console.WriteLine($"Sodium/h: {plan.Targets.SodiumMgPerHour} mg");
```

## Nutrition Targets

### Carbohydrate Recommendations
- **Easy intensity**: 50g/hour
- **Moderate intensity**: 70g/hour
- **Hard intensity**: 90g/hour
- **+10g/hour** for races over 5 hours (moderate/hard intensity)

### Fluid Recommendations
- **Base**: 500ml/hour
- **Hot conditions (≥25°C)**: +200ml/hour
- **Cold conditions (≤5°C)**: -100ml/hour
- **Heavy athletes (>80kg)**: +50ml/hour
- **Light athletes (<60kg)**: -50ml/hour
- **Range**: 300-900ml/hour

### Sodium Recommendations
- **Base**: 400mg/hour
- **Hot conditions (≥25°C)**: +200mg/hour
- **Heavy athletes (>80kg)**: +100mg/hour
- **Range**: 300-1000mg/hour

## Sport Types

- Run
- Bike
- Triathlon

## Intensity Levels

- Easy
- Moderate
- Hard

## Output

The application generates a detailed nutrition plan showing:
- Hourly targets for carbs, fluids, and sodium
- Time-based schedule (default: 20-minute intervals)
- Product quantities and portions
- Total intake throughout the race

## License

This project is available for personal and educational use.

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests.
