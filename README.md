# Race Day Nutrition Planner

A modern application that generates personalized nutrition plans for endurance athletes during races and training sessions. Available as both a web application (React) with an interactive UI and a REST API for programmatic access.

## Overview

This tool calculates optimal carbohydrate, fluid, and sodium intake recommendations based on:
- Athlete characteristics (weight)
- Race parameters (sport type, duration, temperature, intensity)
- Available nutrition products (gels, drinks, bars)

The application generates a time-based schedule showing when and how much of each product to consume during the race.

## Features

- **Interactive Web UI**: User-friendly React web interface for creating nutrition plans
- **Activity Presets**: 9 predefined endurance activities (Sprint Tri, Olympic Tri, Half/Full Ironman, Marathon, etc.)
- **Personalized Calculations**: Adjusts nutrition targets based on athlete weight, race intensity, duration, and temperature
- **Flexible Product Support**: Works with various nutrition products (gels, drinks, bars) from multiple brands
- **Time-Based Schedule**: Generates a minute-by-minute nutrition intake plan with 20-minute intervals
- **Smart Recommendations**: 
  - Increases carb intake for harder efforts and longer durations (5+ hours)
  - Adjusts fluid needs based on temperature and athlete weight
  - Optimizes sodium intake for hot conditions and heavier athletes
- **REST API**: Full programmatic access via REST endpoints with Swagger documentation

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
│   │   ├── ActivityRepository.cs  # Predefined activities
│   │   ├── IProductRepository.cs  # Repository interface
│   │   ├── ProductRepository.cs   # Product data access
│   │   └── Data/                  # Embedded product catalogs (JSON)
│   ├── RaceDay.Web.React/         # React web application (Vite + TypeScript)
│   │   ├── src/
│   │   │   ├── components/        # React components
│   │   │   ├── types.ts           # TypeScript type definitions
│   │   │   ├── api.ts             # API client
│   │   │   ├── utils.ts           # Utility functions (formatDuration, etc.)
│   │   │   ├── App.tsx            # Main application component
│   │   │   └── App.css            # Application styles
│   │   ├── package.json           # NPM dependencies
│   │   └── vite.config.ts         # Vite configuration
│   └── RaceDay.API/               # REST API
│       ├── Program.cs             # API startup and endpoints
│       ├── ApiEndpointExtensions.cs # API endpoint definitions
│       └── Endpoints:
│           ├── /api/products      # Product catalog endpoints
│           ├── /api/activities    # Activity presets endpoints
│           └── /api/plan/generate # Nutrition plan generation (POST)
├── tests/
│   └── RaceDay.Core.Tests/        # Unit tests (59 tests, 100% coverage)
│       ├── NutritionCalculatorTests.cs
│       ├── PlanGeneratorTests.cs
│       ├── ValidationTests.cs
│       └── ActivityRepositoryTests.cs
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
- Node.js 20.x or later (for React web application)
- npm 10.x or later (for React web application)

## Getting Started

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Running the Application

### React Web Application (Recommended)

The React web application provides a modern, interactive UI for creating nutrition plans:

#### Prerequisites
First, start the API server (required for the web app to function):

```shell
# Run the API server (in one terminal)
dotnet run --project src/RaceDay.API/RaceDay.API.csproj
```

The API will run on `http://localhost:5208`

#### Running the React App

```shell
# Navigate to the React app directory
cd src/RaceDay.Web.React

# Install dependencies (first time only)
npm install

# Run the development server
npm run dev
```

Then navigate to `http://localhost:5173` to access the web interface.

#### Building for Production

```shell
# Build the React app for production
cd src/RaceDay.Web.React
npm run build

# The built files will be in the dist/ directory
# Serve them with any static file server
npm run preview
```

#### Web Features:
- Interactive form for athlete profile (body weight)
- Race configuration (sport type, duration, temperature, intensity)
- Dynamic product management (add/remove gels and drinks)
- Server-side nutrition plan calculation via API
- Visual display of targets and intake schedule
- Product browser with filtering capabilities
- Responsive design with modern UI

### REST API (Swagger/OpenAPI)

The application provides a separate REST API service with interactive documentation:

```shell
# Run the API server
dotnet run --project src/RaceDay.API/RaceDay.API.csproj
```

The API will be available at `https://localhost:5001` (or another port shown in console).

#### Access Swagger UI

Navigate to `https://localhost:[port]/swagger/ui` to access the interactive Swagger documentation where you can:
- View all available endpoints
- See request/response schemas
- Test endpoints directly from the browser
- Download OpenAPI specification

#### API Endpoints

**Products**
- **GET /api/products** - Retrieve all products with full details
- **GET /api/products/{id}** - Get a specific product by ID
- **GET /api/products/type/{type}** - Filter products by type (gel, drink, bar)
- **GET /api/products/search?query={query}** - Search products by name or brand

**Activities**
- **GET /api/activities** - Retrieve all predefined activities
- **GET /api/activities/{id}** - Get a specific activity by ID
- **GET /api/activities/type/{sportType}** - Filter activities by sport type (Run, Bike, Triathlon)
- **GET /api/activities/search?query={query}** - Search activities by name

#### Example API Calls

```bash
# Get all products
curl https://localhost:7001/api/products

# Get gel products
curl https://localhost:7001/api/products/type/gel

# Search products by brand
curl https://localhost:7001/api/products/search?query=maurten

# Get all predefined activities
curl https://localhost:7001/api/activities

# Get running activities
curl https://localhost:7001/api/activities/type/Run

# Search activities
curl https://localhost:7001/api/activities/search?query=Marathon
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

For integrating the REST API into your own applications:

```csharp
using HttpClient httpClient = new();

// Get available products from API
var productsResponse = await httpClient.GetAsync("https://localhost:7001/api/products");
var products = await productsResponse.Content.ReadAsAsync<List<ProductInfo>>();

// Get available activities from API
var activitiesResponse = await httpClient.GetAsync("https://localhost:7001/api/activities");
var activities = await activitiesResponse.Content.ReadAsAsync<List<ActivityInfo>>();

// Use core library for calculations
using RaceDay.Core;

var athlete = new AthleteProfile(WeightKg: 89);
var race = new RaceProfile(
    SportType.Triathlon,
    DurationHours: 4.5,
    TemperatureC: 20,
    Intensity: IntensityLevel.Moderate
);

var selectedProducts = products.Take(2).ToList();
var plan = PlanGenerator.Generate(race, athlete, selectedProducts);

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
- **NutritionCalculator**: 100% coverage (14 tests)
  - Nutrition calculations for all intensity levels
  - Temperature and weight adjustments
  - Long race bonuses
- **PlanGenerator**: Full coverage (6 tests)
  - Schedule generation with custom intervals
  - Product validation and exception handling
- **Validation**: Full coverage (22 tests)
  - Race profile, athlete profile, product, and interval validation
  - Boundary conditions and error messages

**Total: 42 tests, all passing**

## Architecture Details

### Three-Tier Architecture

The application follows a **separation of concerns** pattern:

1. **RaceDay.Web.React** (Presentation Layer - React)
   - Modern React web application with TypeScript
   - Built with Vite for fast development and optimized builds
   - Interactive UI with server-side calculations
   - Communicates with API via REST
   - Located at: `src/RaceDay.Web.React`
   - **Components**:
     - `AthleteProfileForm`: Body weight input
     - `RaceDetailsForm`: Activity selection and race parameters
     - `ProductsEditor`: Manage gels and drinks
     - `ProductSelector`: Browse and add products from catalog
     - `PlanResults`: Display nutrition targets and schedule

2. **RaceDay.API** (Application/API Layer)
   - ASP.NET Core REST API with Swagger documentation
   - Product and activity query endpoints
   - Nutrition plan generation endpoint (POST /api/plan/generate)
   - CORS support for frontend integration
   - Located at: `src/RaceDay.API`
   - Access Swagger UI at: `/swagger/ui`

3. **RaceDay.Core** (Business Logic Layer)
   - Pure business logic and calculations
   - `NutritionCalculator`: Calculate hourly nutrition targets
   - `PlanGenerator`: Generate time-based intake schedules
   - Data access through repositories
   - Immutable domain models
   - Located at: `src/RaceDay.Core`

### Deployment Strategy

**Development Setup**:
- Run Web and API on different ports
- Web calls API via configured HttpClient
- Independent development of each layer

**Production Setup**:
- Deploy Web and API as separate services
- Can scale each independently
- Use reverse proxy or load balancer
- Easy maintenance and updates

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
