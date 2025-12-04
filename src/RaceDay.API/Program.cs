using RaceDay.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddSingleton<IProductRepository, ProductRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Race Day Nutrition Planner API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// API Endpoints for Products
app.MapGet("/api/products", async (IProductRepository repository) =>
{
    try
    {
        var products = await repository.GetAllProductsAsync();
        return Results.Ok(products);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error loading products: {ex.Message}");
    }
})
.WithName("GetAllProducts");

app.MapGet("/api/products/{id}", async (string id, IProductRepository repository) =>
{
    try
    {
        var product = await repository.GetProductByIdAsync(id);
        if (product == null)
            return Results.NotFound();
        return Results.Ok(product);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error loading product: {ex.Message}");
    }
})
.WithName("GetProductById");

app.MapGet("/api/products/type/{type}", async (string type, IProductRepository repository) =>
{
    try
    {
        var products = await repository.GetProductsByTypeAsync(type);
        return Results.Ok(products);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error loading products: {ex.Message}");
    }
})
.WithName("GetProductsByType");

app.MapGet("/api/products/search", async (string query, IProductRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(query))
        return Results.BadRequest("Search query is required");
    
    try
    {
        var products = await repository.SearchProductsAsync(query);
        return Results.Ok(products);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error searching products: {ex.Message}");
    }
})
.WithName("SearchProducts");

// API Endpoints for Activities
app.MapGet("/api/activities", async () =>
{
    try
    {
        var activities = await ActivityRepository.GetAllActivitiesAsync();
        return Results.Ok(activities);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error loading activities: {ex.Message}");
    }
})
.WithName("GetAllActivities");

app.MapGet("/api/activities/{id}", async (string id) =>
{
    try
    {
        var activity = await ActivityRepository.GetActivityByIdAsync(id);
        if (activity == null)
            return Results.NotFound();
        return Results.Ok(activity);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error loading activity: {ex.Message}");
    }
})
.WithName("GetActivityById");

app.MapGet("/api/activities/type/{sportType}", async (string sportType) =>
{
    try
    {
        // Parse the sport type
        if (!Enum.TryParse<SportType>(sportType, ignoreCase: true, out var parsedSportType))
            return Results.BadRequest($"Invalid sport type. Valid values: Run, Bike, Triathlon");

        var activities = await ActivityRepository.GetActivitiesBySportTypeAsync(parsedSportType);
        return Results.Ok(activities);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error loading activities: {ex.Message}");
    }
})
.WithName("GetActivitiesByType");

app.MapGet("/api/activities/search", async (string query) =>
{
    if (string.IsNullOrWhiteSpace(query))
        return Results.BadRequest("Search query is required");
    
    try
    {
        var activities = await ActivityRepository.SearchActivitiesAsync(query);
        return Results.Ok(activities);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error searching activities: {ex.Message}");
    }
})
.WithName("SearchActivities");

app.Run();
