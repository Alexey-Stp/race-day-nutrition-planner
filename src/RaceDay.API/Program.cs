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
app.MapGet("/api/products", async () =>
{
    var products = await ProductRepository.GetAllProductsAsync();
    return Results.Ok(products);
})
.WithName("GetAllProducts");

app.MapGet("/api/products/{id}", async (string id) =>
{
    var product = await ProductRepository.GetProductByIdAsync(id);
    if (product == null)
        return Results.NotFound();
    return Results.Ok(product);
})
.WithName("GetProductById");

app.MapGet("/api/products/type/{type}", async (string type) =>
{
    var products = await ProductRepository.GetProductsByTypeAsync(type);
    return Results.Ok(products);
})
.WithName("GetProductsByType");

app.MapGet("/api/products/search", async (string query) =>
{
    if (string.IsNullOrWhiteSpace(query))
        return Results.BadRequest("Search query is required");
    
    var products = await ProductRepository.SearchProductsAsync(query);
    return Results.Ok(products);
})
.WithName("SearchProducts");

app.Run();
