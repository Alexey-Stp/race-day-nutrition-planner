# Race Day Nutrition Planner

A modern .NET application suite that generates personalized nutrition plans for endurance athletes during races and training sessions. Available as both a web application (Blazor) and CLI tool.

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

## Architecture

This solution follows clean architecture principles with clear separation of concerns:

### Project Structure

```
RaceDayNutritionPlanner/
├── src/
│   ├── RaceDay.Core/              # Core business logic library
│   │   ├── Models.cs              # Domain models and records
│   │   ├── NutritionCalculator.cs # Nutrition target calculations
│   │   ├── PlanGenerator.cs       # Schedule generation
│   │   ├── NutritionConstants.cs  # Configuration constants
│   │   ├── Validation.cs          # Input validation
│   │   ├── Exceptions.cs          # Custom exceptions
│   │   ├── IProductRepository.cs  # Repository interface
│   │   ├── ProductRepository.cs   # Product data access
│   │   └── Data/                  # Embedded product catalogs (JSON)
│   ├── RaceDay.CLI/               # Console application
│   │   └── Program.cs             # CLI entry point
│   └── RaceDay.Web/               # Blazor Server web application
│       ├── Program.cs             # Web API and startup
│       └── Components/            # Blazor components
├── tests/
│   └── RaceDay.Core.Tests/        # Unit tests for core logic
└── RaceDayNutritionPlanner.sln
```

### Key Design Patterns

- **Repository Pattern**: `IProductRepository` abstracts data access
- **Dependency Injection**: Services registered in web application
- **Immutable Records**: All models use C# records for immutability
- **Static Utilities**: Pure functions for calculations
- **Constants Configuration**: Centralized in `NutritionConstants`
- **Custom Exceptions**: Type-safe error handling

### Core Components

#### NutritionCalculator
Calculates hourly nutrition targets based on:
- **Carbohydrates**: 50-100g/hour depending on intensity and duration
- **Fluids**: 300-900ml/hour adjusted for temperature and athlete weight
- **Sodium**: 300-1000mg/hour adjusted for temperature and athlete weight

#### PlanGenerator
Creates time-based intake schedules with:
- Configurable time intervals (default: 20 minutes)
- Product portion calculations
- Total nutrition summaries

#### Validation
Validates input data to ensure:
- Race duration is realistic (0-24 hours)
- Temperature is reasonable (-20 to 50°C)
- Athlete weight is valid (>0, <250 kg)
- Products have required properties

## Requirements

- .NET 9.0 SDK or later

## Getting Started

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the CLI application
dotnet run --project src/RaceDay.CLI/RaceDay.CLI.csproj

# Run the web application
dotnet run --project src/RaceDay.Web/RaceDay.Web.csproj
# Then navigate to https://localhost:5001
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

## Testing

The solution includes comprehensive unit tests for the core business logic:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests for specific project
dotnet test tests/RaceDay.Core.Tests/RaceDay.Core.Tests.csproj
```

Current test coverage includes:
- Nutrition calculation logic for all intensity levels
- Temperature and weight adjustments
- Long race bonuses
- Input validation
- Exception handling

## API Endpoints (Web Application)

The web application exposes REST API endpoints:

- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `GET /api/products/type/{type}` - Get products by type (gel/drink/bar)
- `GET /api/products/search?query={query}` - Search products

## Error Handling

The application uses custom exceptions for type-safe error handling:

- **`MissingProductException`**: Thrown when required product types are missing
- **`ValidationException`**: Thrown when input validation fails
- **`RaceDayException`**: Base exception for application-specific errors

## Code Quality

This project follows software engineering best practices:

✅ **SOLID Principles**
- Single Responsibility: Each class has one clear purpose
- Open/Closed: Extensible through interfaces
- Dependency Inversion: Depends on abstractions (IProductRepository)

✅ **Clean Code**
- Immutable data models using C# records
- Pure functions for calculations
- Comprehensive XML documentation
- Meaningful naming conventions

✅ **Testing**
- Unit tests for business logic
- Test-driven development approach
- 100% core logic test coverage

✅ **Architecture**
- Separation of concerns
- Dependency injection
- Repository pattern
- Async/await for I/O operations

## License

This project is available for personal and educational use.

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests.

Please ensure:
- All tests pass (`dotnet test`)
- Code follows existing patterns
- New features include unit tests
- Public APIs have XML documentation
