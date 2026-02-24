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
- **Activity Presets**: 3 core endurance activities (Run, Bike, Triathlon) with customizable durations
- **Personalized Calculations**: Adjusts nutrition targets based on athlete weight, race intensity, duration, and temperature
- **Individual Product Selection**:
  - Choose specific products via checkboxes, not just entire brands
  - Products grouped by type (Drinks, Gels, Bars, Chews, Recovery)
  - Global search to filter products
  - "Select All / Clear" controls per group
  - Collapsible interface for compact layout
- **Brand Filtering**: Filter products by brand for quick selection (e.g., "SiS", "Maurten")
  - Automatic product type exclusions based on sport (e.g., runners don't carry bottles)
- **Time-Based Schedule**: Generates a minute-by-minute nutrition intake plan with 20-minute intervals
- **Smart Recommendations**:
  - Increases carb intake for harder efforts and longer durations (5+ hours)
  - Adjusts fluid needs based on temperature and athlete weight
  - Optimizes sodium intake for hot conditions and heavier athletes
  - **Tracks caffeine intake** and displays caffeine content from selected products
  - Enable/disable caffeine products via checkbox
- **Comprehensive Testing**: 212 unit tests covering all core functionality with regression test suite
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
│   └── RaceDay.Core.Tests/        # Unit tests (212 tests)
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

### For Docker Deployment (Recommended)
- Docker Desktop or Docker Engine
- Docker Compose

### For Local Development
- .NET 10.0 SDK or later
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

## Docker Deployment

### Running with Docker (Recommended for Production)

The application can be easily deployed using Docker containers, which is ideal for Azure Linux Container deployments and local testing.

#### Prerequisites
- Docker Desktop or Docker Engine installed
- Docker Compose (included with Docker Desktop)

#### Quick Start with Docker Compose

```bash
# Build and start all services (API + Web)
docker compose up --build

# Or run in detached mode
docker compose up --build -d
```

> **Note**: If using Docker Compose v1, use `docker-compose` instead of `docker compose`

The application will be available at:
- **Web Application**: http://localhost:8080
- **API**: http://localhost:5208
- **Swagger Documentation** (Development only): http://localhost:5208/swagger

To stop the containers:

```bash
docker compose down
```

#### Building Individual Docker Images

**Build API Image:**
```bash
docker build -t raceday-api -f src/RaceDay.API/Dockerfile .
docker run -p 5208:8080 raceday-api
```

**Build Web Image:**
```bash
cd src/RaceDay.Web.React
docker build -t raceday-web .
docker run -p 8080:80 raceday-web
```

#### Azure Container Instances Deployment

The Docker images are optimized for deployment to Azure Container Instances or Azure App Service (Linux containers).

**Deploy to Azure:**

1. **Build and tag images for Azure Container Registry:**
   ```bash
   # Login to Azure
   az login
   
   # Create Azure Container Registry (if needed)
   az acr create --resource-group <resource-group> --name <registry-name> --sku Basic
   
   # Login to ACR
   az acr login --name <registry-name>
   
   # Build and push API image
   docker build -t <registry-name>.azurecr.io/raceday-api:latest -f src/RaceDay.API/Dockerfile .
   docker push <registry-name>.azurecr.io/raceday-api:latest
   
   # Build and push Web image
   cd src/RaceDay.Web.React
   docker build -t <registry-name>.azurecr.io/raceday-web:latest .
   docker push <registry-name>.azurecr.io/raceday-web:latest
   ```

2. **Deploy to Azure Container Instances:**
   ```bash
   # Create container group with both services
   az container create \
     --resource-group <resource-group> \
     --name raceday-nutrition-planner \
     --image <registry-name>.azurecr.io/raceday-api:latest \
     --registry-login-server <registry-name>.azurecr.io \
     --registry-username <acr-username> \
     --registry-password <acr-password> \
     --dns-name-label raceday-api \
     --ports 8080
   ```

**Alternatively, deploy to Azure App Service:**

```bash
# Create App Service Plan (Linux)
az appservice plan create --name raceday-plan --resource-group <resource-group> --is-linux --sku B1

# Create Web App for Containers
az webapp create --resource-group <resource-group> --plan raceday-plan --name raceday-api --deployment-container-image-name <registry-name>.azurecr.io/raceday-api:latest

# Configure container registry credentials
az webapp config container set --name raceday-api --resource-group <resource-group> \
  --docker-custom-image-name <registry-name>.azurecr.io/raceday-api:latest \
  --docker-registry-server-url https://<registry-name>.azurecr.io \
  --docker-registry-server-user <acr-username> \
  --docker-registry-server-password <acr-password>
```

#### VS Code Tasks

For convenience, Docker tasks are available in VS Code:
- **Docker: Build & Start** - Build and start all containers
- **Docker: Stop** - Stop all containers
- **Docker: View Logs** - View container logs
- **Docker: Build, Start & Open Browser** - Complete workflow with browser launch

Access these via `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac) → "Tasks: Run Task"

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
- **Individual product selection** with checkboxes grouped by type
- **Brand filtering** to quickly select products from favorite brands
- **Caffeine control** - enable/disable caffeinated products
- Server-side nutrition plan calculation via API
- Visual display of targets, caffeine totals, and intake schedule
- Product browser with search and filtering capabilities
- Responsive design with modern, compact UI

### REST API (Swagger/OpenAPI)

The application provides a separate REST API service with interactive documentation:

```shell
# Run the API server
dotnet run --project src/RaceDay.API/RaceDay.API.csproj
```

The API will be available at `https://localhost:5001` (or another port shown in console).

#### Access Swagger UI

Navigate to `https://localhost:[port]/swagger` to access the interactive Swagger documentation where you can:
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

**Nutrition Plan Generation**
- **POST /api/plan/generate** - Generate a personalized nutrition plan with shopping list
  - Returns nutrition targets, time-based schedule, totals, and aggregated product summaries
  - Shopping list shows total quantities needed per product for easy race day preparation
  - Supports optional brand filtering via `filter` parameter

**Metadata** (UI Support)
- **GET /api/metadata** - Get all UI metadata (temperatures, intensities, defaults)
- **GET /api/metadata/temperatures** - Get temperature condition metadata with ranges and effects
- **GET /api/metadata/intensities** - Get intensity level metadata with icons, carb ranges, and effects
- **GET /api/metadata/defaults** - Get default values for the application
- **GET /api/metadata/configuration** - Get all nutrition configuration including sport-specific parameters and thresholds
- **POST /api/metadata/targets** - Calculate nutrition targets for specific athlete and race parameters

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

# Generate nutrition plan with shopping list
curl -X POST https://localhost:7001/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": 0,
    "durationHours": 4,
    "temperatureC": 25,
    "intensity": 2,
    "products": [
      {"name": "Energy Gel", "productType": "gel", "carbsG": 25, "sodiumMg": 100, "volumeMl": 0},
      {"name": "Sports Drink", "productType": "drink", "carbsG": 30, "sodiumMg": 300, "volumeMl": 500}
    ]
  }'

# Generate plan with brand filter (automatically excludes drinks for running)
curl -X POST https://localhost:7001/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Run",
    "durationHours": 3,
    "temperatureC": 20,
    "intensity": "Moderate",
    "filter": {
      "brand": "SiS"
    }
  }'

# Generate plan with brand filter and explicit type exclusions
curl -X POST https://localhost:7001/api/plan/generate \
  -H "Content-Type: application/json" \
  -d '{
    "athleteWeightKg": 75,
    "sportType": "Bike",
    "durationHours": 3,
    "temperatureC": 20,
    "intensity": "Moderate",
    "filter": {
      "brand": "SiS",
      "excludeTypes": ["bar"]
    }
  }'
```

The response includes a `productSummaries` array with the shopping list:
```json
{
  "productSummaries": [
    {"productName": "Energy Gel", "totalPortions": 12},
    {"productName": "Sports Drink", "totalPortions": 6}
  ]
}
```

### Brand Filtering

The API supports filtering products by brand without needing to specify individual products. This is useful when you want to use only products from a specific brand (e.g., "SiS", "Maurten").

#### Sport-Specific Automatic Exclusions

When using brand filtering, certain product types are automatically excluded based on the sport:

- **Run**: Automatically excludes `drink` and `recovery` types (runners typically don't carry bottles)
- **Bike**: No automatic exclusions (cyclists can easily carry bottles)
- **Triathlon**: No automatic exclusions (bike portion allows for carrying bottles)

#### Using Brand Filters

The `filter` parameter accepts:
- `brand`: String - Filter by brand name (e.g., "SiS", "Maurten") - case insensitive
- `excludeTypes`: Array of strings - Explicitly exclude product types (e.g., ["bar", "gel"])

**Example - Running with SiS brand:**
```json
{
  "athleteWeightKg": 75,
  "sportType": "Run",
  "durationHours": 3,
  "filter": {
    "brand": "SiS"
  }
}
```
This will use only SiS gels and bars (drinks automatically excluded for running).

**Example - Cycling with explicit exclusions:**
```json
{
  "athleteWeightKg": 75,
  "sportType": "Bike",
  "durationHours": 4,
  "filter": {
    "brand": "Maurten",
    "excludeTypes": ["bar"]
  }
}
```
This will use only Maurten gels and drinks (bars explicitly excluded).

For detailed examples, see [Brand Filter Usage Examples](example-outputs/brand-filter-usage.md).

## Quick API Testing

For easy API testing with predefined scenarios (half/full triathlon, marathons, bike rides), see **[TESTING.md](TESTING.md)** which includes:

- **Quick test script**: `./test-nutrition-plan.sh [scenario]`
- **HTTP test file**: `src/RaceDay.API/RaceDay.API.http` (use with VS Code REST Client)
- **Comprehensive documentation** for testing and validation workflow

Example:
```bash
# Start the API
dotnet run --project src/RaceDay.API/RaceDay.API.csproj

# Run a test scenario
./test-nutrition-plan.sh half-triathlon
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
- **Shopping list**: Aggregated product summaries showing total quantities needed per product

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
- **NutritionCalculatorTests**: Target calculation for all intensity levels, temperature and weight adjustments
- **PlanGeneratorTests**: Core schedule generation, product validation, exception handling
- **ValidationTests**: Race profile, athlete profile, product, and interval validation; boundary conditions
- **ActivityRepositoryTests**: Activity catalogue access
- **AlgorithmImprovementTests**: Algorithm v2 regression tests
- **AdvancedPlanGeneratorTests**: Advanced scheduling scenarios (caffeine, coverage, front-load)

**Total: 212 tests, all passing**

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
   - Access Swagger UI at: `/swagger`

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

### UI Screenshot Testing

Pull requests automatically generate UI screenshots for both mobile (390×844) and desktop (1440×900) viewports using Playwright.

**Viewing Screenshots:**
- Screenshots are automatically posted in PR comments with inline previews
- Full Playwright HTML reports are published to GitHub Pages
- Links appear within minutes of opening/updating a PR

**Adding New Routes to Screenshot Tests:**
1. Edit `src/RaceDay.Web.React/e2e/ui-screenshots.spec.ts`
2. Add a new test case following the existing pattern
3. Use `testInfo.project.name` to separate mobile/desktop paths
4. Wait for `networkidle` and key UI elements before capturing

**Running Locally:**
```bash
cd src/RaceDay.Web.React
npm install
npm run build
npm run preview &  # Start preview server
npx playwright test  # Run tests for both viewports
npx playwright show-report  # View HTML report
```

**How It Works:**
- The `ui_screenshots` job in `.github/workflows/pr_check.yml` runs on every PR
- Both mobile and desktop screenshots are captured using Playwright projects
- Results are published to `gh-pages` branch under `ui-preview/pr-<number>/latest/`
- A PR comment is created/updated with direct links and inline images
- For fork PRs, screenshots are generated but not published (artifacts only)

