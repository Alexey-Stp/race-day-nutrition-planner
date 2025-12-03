using RaceDay.Web.Components;
using RaceDay.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
app.MapGet("/api/products", async () =>
{
    var products = await ProductRepository.GetAllProductsAsync();
    return Results.Ok(products);
});

app.MapGet("/api/products/{id}", async (string id) =>
{
    var product = await ProductRepository.GetProductByIdAsync(id);
    if (product == null)
        return Results.NotFound();
    return Results.Ok(product);
});

app.MapGet("/api/products/type/{type}", async (string type) =>
{
    var products = await ProductRepository.GetProductsByTypeAsync(type);
    return Results.Ok(products);
});

app.MapGet("/api/products/search", async (string query) =>
{
    if (string.IsNullOrWhiteSpace(query))
        return Results.BadRequest("Search query is required");
    
    var products = await ProductRepository.SearchProductsAsync(query);
    return Results.Ok(products);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
