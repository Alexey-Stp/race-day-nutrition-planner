using RaceDay.Web.Components;
using RaceDay.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();

// Register application services
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddScoped<INutritionPlanService, NutritionPlanService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// API Endpoints
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
});

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
});

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
});

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
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
