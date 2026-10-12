using RaceDay.API.Infrastructure;
using Serilog;

// Console logger for anything logged before the host's logger exists
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Replace the default MEL providers with Serilog; sinks come from configuration
// (Console in all environments, rolling File only in appsettings.Development.json).
// preserveStaticLogger: each host owns its logger, so hosts built concurrently (integration tests)
// never contend over freezing a shared static bootstrap logger.
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName(),
    preserveStaticLogger: true);

builder.ConfigurePlatformPort();
builder.AddRaceDayApi();

var app = builder.Build();

app.UseRaceDayApi();

await app.RunAsync();
