using RaceDay.Core;
using RaceDay.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Configure CORS to allow access from any origin
// This is intentional for a public API designed to be consumed by any client
// The API does not handle sensitive data or authentication
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

app.Run();
