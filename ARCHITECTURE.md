# Architecture Documentation

## Overview

The Race Day Nutrition Planner is a modern .NET 9 application that follows clean architecture principles with clear separation of concerns. The solution consists of three main projects:

1. **RaceDay.Core** - Core business logic library
2. **RaceDay.CLI** - Console application
3. **RaceDay.Web** - Blazor Server web application

## Design Principles

### SOLID Principles

#### Single Responsibility Principle (SRP)
- Each class has one clear purpose
- `NutritionCalculator` - only calculates nutrition targets
- `PlanGenerator` - only generates nutrition plans
- `ProductRepository` - only handles product data access
- `Validation` - only validates input data

#### Open/Closed Principle (OCP)
- Core logic is closed for modification but open for extension
- Interface-based design allows new implementations (`IProductRepository`, `INutritionPlanService`)
- New product types can be added without changing existing code

#### Liskov Substitution Principle (LSP)
- Interfaces can be substituted with any implementation
- `IProductRepository` implementations are interchangeable

#### Interface Segregation Principle (ISP)
- Focused interfaces with minimal methods
- `IProductRepository` only includes product access methods
- `INutritionPlanService` only includes plan generation

#### Dependency Inversion Principle (DIP)
- High-level modules depend on abstractions
- Web application depends on `IProductRepository`, not concrete implementation
- Enables testability and flexibility

## Architecture Layers

### Domain Layer (RaceDay.Core)

Contains business entities, business logic, and interfaces.

#### Models
- Immutable records for data integrity
- Strong typing for type safety
- No business logic in models (anemic domain model approach)

Key models:
- `AthleteProfile` - Athlete characteristics
- `RaceProfile` - Race/session details
- `Product` - Nutrition product
- `NutritionTargets` - Calculated targets
- `RaceNutritionPlan` - Complete plan with schedule

#### Business Logic

**NutritionCalculator**
```csharp
public static class NutritionCalculator
{
    public static NutritionTargets CalculateTargets(RaceProfile race, AthleteProfile athlete)
}
```
- Pure static functions
- No side effects
- Deterministic calculations
- Uses `NutritionConstants` for configuration

**PlanGenerator**
```csharp
public static class PlanGenerator
{
    public static RaceNutritionPlan Generate(
        RaceProfile race,
        AthleteProfile athlete,
        List<Product> products,
        int intervalMin = 20)
}
```
- Orchestrates plan creation
- Validates inputs using `Validation` class
- Throws custom exceptions for errors

#### Configuration

**NutritionConstants**
- Centralized configuration values
- Organized in nested static classes
- Easy to modify without touching logic
- Self-documenting through organization

#### Validation

**Validation**
```csharp
public static class Validation
{
    public static void ValidateRaceProfile(RaceProfile race)
    public static void ValidateAthleteProfile(AthleteProfile athlete)
    public static void ValidateProduct(Product product)
    public static void ValidateInterval(int intervalMin)
}
```
- Centralized validation logic
- Throws `ValidationException` with property name
- Validates business rules and constraints

#### Exception Handling

Custom exception hierarchy:
```
Exception
└── RaceDayException (base)
    ├── MissingProductException
    └── ValidationException
```

Benefits:
- Type-safe error handling
- Clear error messages
- Easy to catch specific errors
- Additional context in exceptions

#### Repository Pattern

**IProductRepository**
```csharp
public interface IProductRepository
{
    Task<List<ProductInfo>> GetAllProductsAsync();
    Task<List<ProductInfo>> GetProductsByTypeAsync(string productType);
    Task<ProductInfo?> GetProductByIdAsync(string id);
    Task<List<ProductInfo>> SearchProductsAsync(string query);
}
```

**ProductRepository**
- Loads products from embedded JSON resources
- Thread-safe lazy initialization using `SemaphoreSlim`
- Truly async implementation
- Caches products in memory after first load

### Application Layer (RaceDay.CLI, RaceDay.Web)

#### CLI Application
- Simple demonstration of core functionality
- Direct instantiation of models
- Minimal error handling for demonstration

#### Web Application
- Blazor Server with interactive components
- Dependency injection for services
- RESTful API endpoints
- Proper error handling with HTTP status codes

**Dependency Registration**
```csharp
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddScoped<INutritionPlanService, NutritionPlanService>();
```

**API Endpoints**
```
GET /api/products - Get all products
GET /api/products/{id} - Get product by ID
GET /api/products/type/{type} - Get products by type
GET /api/products/search?query={query} - Search products
```

All endpoints include:
- Proper error handling
- HTTP status codes
- Exception to problem details conversion

## Data Flow

### Nutrition Plan Generation

```
User Input (Race, Athlete, Products)
    ↓
[Validation]
    ↓
[NutritionCalculator.CalculateTargets]
    ↓
NutritionTargets (Carbs, Fluids, Sodium per hour)
    ↓
[PlanGenerator.Generate]
    ↓
RaceNutritionPlan (with Schedule)
    ↓
Display to User
```

### Product Loading

```
Application Startup
    ↓
[ProductRepository.GetAllProductsAsync]
    ↓
Check Cache
    ↓
[LoadProductsFromJsonFiles] (if not cached)
    ↓
Load Embedded Resources
    ↓
Deserialize JSON
    ↓
Cache in Memory
    ↓
Return Products
```

## Testing Strategy

### Unit Tests

Located in `tests/RaceDay.Core.Tests/`

**Test Coverage:**
- ✅ Nutrition calculations for all intensity levels
- ✅ Temperature adjustments (hot/cold)
- ✅ Weight adjustments (heavy/light athletes)
- ✅ Long race bonuses
- ✅ Clamping to safe ranges
- ✅ Plan generation with valid inputs
- ✅ Exception handling for missing products
- ✅ Schedule interval validation

**Test Approach:**
- Arrange-Act-Assert pattern
- Descriptive test names
- Independent tests (no shared state)
- Fast execution (no I/O)

### Future Testing Improvements

- Integration tests for Web API
- UI component tests for Blazor
- Performance tests for large datasets
- Repository tests with mock data

## Security Considerations

### Input Validation
- All user inputs validated before processing
- Range checks prevent unreasonable values
- Type safety through strong typing

### Data Integrity
- Immutable records prevent accidental modifications
- No mutable state in core logic
- Thread-safe repository implementation

### Error Handling
- No sensitive information in error messages
- Proper exception types for different scenarios
- Graceful degradation in Web API

## Performance Considerations

### Caching
- Products loaded once and cached in memory
- Thread-safe lazy initialization
- No unnecessary reloading

### Calculations
- Pure functions enable optimization
- No I/O during calculations
- Minimal object allocations

### Async/Await
- Proper async implementation in repository
- No blocking calls
- Scalable for web workloads

## Extensibility Points

### Adding New Product Types
1. Add product data to JSON file in `Data/`
2. Mark file as embedded resource
3. No code changes required

### Adding New Calculation Rules
1. Update `NutritionConstants` if needed
2. Modify calculation methods in `NutritionCalculator`
3. Add corresponding unit tests

### Adding New Repositories
1. Implement `IProductRepository` interface
2. Register in DI container
3. Can use different data sources (database, API, etc.)

### Adding New Services
1. Define interface (e.g., `INewService`)
2. Implement service class
3. Register in DI container
4. Inject where needed

## Best Practices Implemented

### Code Quality
- ✅ XML documentation on all public APIs
- ✅ Meaningful naming conventions
- ✅ Small, focused methods
- ✅ No magic numbers (use constants)
- ✅ Consistent code style

### Architecture
- ✅ Clean separation of concerns
- ✅ Dependency injection
- ✅ Interface-based design
- ✅ Immutable data structures
- ✅ Pure functions where possible

### Error Handling
- ✅ Custom exception types
- ✅ Validation at boundaries
- ✅ Proper error messages
- ✅ No swallowed exceptions

### Testing
- ✅ Comprehensive unit tests
- ✅ Test-driven development approach
- ✅ High code coverage
- ✅ Fast, reliable tests

## Future Enhancements

### Architecture
- [ ] Add logging infrastructure (ILogger)
- [ ] Implement health checks for web app
- [ ] Add caching layer for API responses
- [ ] Implement user preferences persistence

### Features
- [ ] Support for custom product formulas
- [ ] Multi-day event planning
- [ ] Nutrition tracking and comparison
- [ ] Integration with fitness platforms

### Technical
- [ ] Add OpenAPI/Swagger documentation
- [ ] Implement rate limiting on API
- [ ] Add monitoring and telemetry
- [ ] Database storage for user plans

## Conclusion

This architecture provides a solid foundation for a maintainable, testable, and extensible nutrition planning application. The clean separation of concerns, adherence to SOLID principles, and comprehensive testing ensure the codebase can evolve with changing requirements while maintaining quality and reliability.
