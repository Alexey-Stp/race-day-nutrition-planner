namespace RaceDay.API.Infrastructure;

using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using RaceDay.API.Metadata;
using RaceDay.Core.Repositories;
using RaceDay.Core.Services;

/// <summary>
/// Service registration and HTTP pipeline for the API host.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Enums must be sent by name; integer values are rejected so undefined numeric values never bind.
    /// </summary>
    public const bool AllowIntegerEnumValues = false;

    /// <summary>
    /// Railway injects the listening port as <c>PORT</c>. Honour it unless the URL binding was set explicitly
    /// (<c>ASPNETCORE_URLS</c> / <c>--urls</c>); otherwise <c>ASPNETCORE_HTTP_PORTS</c> (8080 in the container) applies.
    /// </summary>
    public static void ConfigurePlatformPort(this WebApplicationBuilder builder)
    {
        var port = Environment.GetEnvironmentVariable("PORT");
        if (int.TryParse(port, out var parsed) && parsed is > 0 and <= 65535
            && string.IsNullOrWhiteSpace(builder.Configuration[WebHostDefaults.ServerUrlsKey]))
        {
            builder.WebHost.UseSetting(WebHostDefaults.HttpPortsKey, parsed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Registers API services.
    /// </summary>
    public static void AddRaceDayApi(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ApiLimits.MaxRequestBodyBytes);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: AllowIntegerEnumValues));
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Clients reach the app through the platform proxy; take the client IP from the last X-Forwarded-For hop
        // so per-IP rate limiting partitions by caller rather than by proxy.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        AddCorsPolicy(services);
        AddPlanRateLimiting(builder);

        services.AddHealthChecks().AddCheck<CatalogueHealthCheck>("catalogue");

        // Core services are stateless (catalogues are cached read-only snapshots), so singletons are safe.
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IActivityRepository, ActivityRepository>();
        services.AddSingleton<INutritionPlanService, NutritionPlanService>();
        services.AddSingleton<IUIMetadataService, UIMetadataService>();
        services.AddSingleton<IConfigurationMetadataService, ConfigurationMetadataService>();
    }

    /// <summary>
    /// Development allows loopback origins only (the Vite dev server). Elsewhere only the configured
    /// <c>Cors:AllowedOrigins</c> are allowed; when none are configured cross-origin requests are denied and a
    /// warning is logged at startup. Returns an empty list in Development.
    /// </summary>
    public static IReadOnlyList<string> ResolveCorsOrigins(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return [];

        var configured = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()?.AllowedOrigins ?? [];
        return configured.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim().TrimEnd('/')).ToArray();
    }

    private static void AddCorsPolicy(IServiceCollection services)
    {
        services.AddCors();
        // Bound lazily so the final configuration (including host/test overrides) is used.
        services.AddOptions<CorsOptions>().Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) =>
        {
            var origins = ResolveCorsOrigins(configuration, environment);
            options.AddPolicy(CorsSettings.PolicyName, policy =>
            {
                if (environment.IsDevelopment())
                    policy.SetIsOriginAllowed(IsLoopbackOrigin);
                else
                    policy.WithOrigins([.. origins]);
                policy.AllowAnyMethod().AllowAnyHeader();
            });
        });
    }

    private static bool IsLoopbackOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback;

    private static void AddPlanRateLimiting(WebApplicationBuilder builder)
    {
        builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(ApiLimits.PlanRateLimitPolicy, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                var clientKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, settings.PermitLimit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, settings.WindowSeconds)),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Rate limit exceeded. Please wait before generating another plan."
                    }
                });
            };
        });
    }

    /// <summary>
    /// Configures the middleware pipeline and maps all endpoints.
    /// </summary>
    public static void UseRaceDayApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() && ResolveCorsOrigins(app.Configuration, app.Environment).Count == 0)
        {
            app.Logger.LogWarning(
                "No CORS origins configured (Cors:AllowedOrigins); cross-origin requests are denied. " +
                "Set Cors__AllowedOrigins__0 to the frontend origin to allow browser access from it.");
        }

        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Race Day Nutrition Planner API v1"));
        }

        // In containers (Railway, docker-compose) TLS is terminated by the platform edge / reverse proxy and the
        // app listens on plain HTTP, so only redirect when running directly on a developer machine.
        if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }

        app.UseCors(CorsSettings.PolicyName);

        // Reject declared oversize bodies up front; Kestrel's MaxRequestBodySize covers chunked bodies.
        app.Use(async (context, next) =>
        {
            if (context.Request.ContentLength > ApiLimits.MaxRequestBodyBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status413PayloadTooLarge,
                        Title = "Request body too large",
                        Detail = $"The request body exceeds the {ApiLimits.MaxRequestBodyBytes} byte limit."
                    }
                });
                return;
            }
            await next(context);
        });

        app.UseRateLimiter();

        app.MapProductEndpoints();
        app.MapActivityEndpoints();
        app.MapPlanEndpoints();
        app.MapMetadataEndpoints();
        app.MapHealthChecks("/health").DisableRateLimiting();
    }
}
