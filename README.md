# Race Day Nutrition Planner

A .NET application that generates personalized nutrition plans for endurance athletes during races and training sessions. Available as both a web application with an interactive UI and a console application for programmatic use.

## Overview

This tool calculates optimal carbohydrate, fluid, and sodium intake recommendations based on:
- Athlete characteristics (weight)
- Race parameters (sport type, duration, temperature, intensity)
- Available nutrition products (gels, drinks, bars)

The application generates a time-based schedule showing when and how much of each product to consume during the race.

## Features

- **Interactive Web UI**: User-friendly Blazor web interface for creating nutrition plans
- **Personalized Calculations**: Adjusts nutrition targets based on athlete weight, race intensity, duration, and temperature
- **Flexible Product Support**: Works with various nutrition products (gels, drinks, bars)
- **Time-Based Schedule**: Generates a minute-by-minute nutrition intake plan
- **Smart Recommendations**: 
  - Increases carb intake for harder efforts and longer durations (5+ hours)
  - Adjusts fluid needs based on temperature and athlete weight
  - Optimizes sodium intake for hot conditions and heavier athletes
- **Programmatic API**: Core library can be integrated into other applications

## Project Structure

```
RaceDayNutritionPlanner/
├── src/
│   ├── RaceDay.Core/          # Core logic library
│   │   ├── Models.cs          # Data models and records
│   │   ├── NutritionCalculator.cs  # Target calculation logic
│   │   └── PlanGenerator.cs   # Schedule generation logic
│   ├── RaceDay.Web/           # Blazor web application
│   │   ├── Components/        # Razor components and pages
│   │   ├── wwwroot/          # Static web assets
│   │   └── Program.cs        # Web app entry point
│   └── RaceDay.CLI/           # Console application
│       └── Program.cs         # Entry point and example usage
└── RaceDayNutritionPlanner.sln
```

## Requirements

- .NET 9.0 SDK or later

## Building the Project

```shell
# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

## Running the Application

### Web Application (Recommended)

The web application provides an interactive UI for creating nutrition plans:

```shell
# Run the web application
dotnet run --project src/RaceDay.Web/RaceDay.Web.csproj
```

Then navigate to `https://localhost:5001` (or the URL shown in the console) to access the web interface.

#### Web Features:
- Interactive form for athlete profile (body weight)
- Race configuration (sport type, duration, temperature, intensity)
- Dynamic product management (add/remove gels and drinks)
- Real-time nutrition plan calculation
- Visual display of targets and intake schedule

### CLI Application

For programmatic use or integration into scripts:

```shell
# Run the CLI application
dotnet run --project src/RaceDay.CLI/RaceDay.CLI.csproj
```

## Usage

### Web Application Usage

1. Open the web application in your browser
2. Enter your **Athlete Profile**:
   - Body weight in kilograms
3. Configure **Race Details**:
   - Sport type (Running, Cycling, or Triathlon)
   - Duration in hours
   - Temperature in Celsius
   - Intensity level (Easy, Moderate, or Hard)
4. Manage **Available Products**:
   - Add or remove gels and drinks
   - Configure carbohydrates, sodium, and volume for each product
5. Click **Calculate Nutrition Plan** to generate your personalized plan
6. Review the results:
   - Hourly nutrition targets
   - Total intake over the race
   - Detailed 20-minute interval schedule

### Programmatic Usage Example

For integrating the core library into your own applications:

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
