using RaceDay.Core;
using RaceDay.API;

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
// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Race Day Nutrition Planner API v1");
});

// Only redirect to HTTPS in production when not in container (Azure handles SSL termination)
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");

// Map API endpoints
app.MapProductEndpoints();
app.MapActivityEndpoints();
app.MapPlanEndpoints();

app.Run();
