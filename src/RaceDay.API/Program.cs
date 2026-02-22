using RaceDay.Core.Services;
using RaceDay.Core.Repositories;
using RaceDay.API;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Configure JSON serialization to use string values for enums instead of numeric
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Configure CORS to allow access from any origin
// This is intentional for a public API designed to be consumed by any client
// The API does not handle sensitive data or authentication
// S5122: Permissive CORS is acceptable for this public, read-only nutrition planning API
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

// Support Railway's dynamic PORT injection (falls back to 8080 for docker-compose)
builder.WebHost.UseUrls($"http://+:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

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

// Only redirect to HTTPS when not in a container (Azure handles SSL termination at the gateway)
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");

// Map API endpoints
app.MapProductEndpoints();
app.MapActivityEndpoints();
app.MapPlanEndpoints();
app.MapMetadataEndpoints();

await app.RunAsync();
