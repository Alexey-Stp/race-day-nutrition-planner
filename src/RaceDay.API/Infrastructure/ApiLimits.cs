namespace RaceDay.API.Infrastructure;

/// <summary>
/// Transport-level limits for the API host.
/// </summary>
public static class ApiLimits
{
    /// <summary>Maximum accepted request body (64 KB) — ample for 50 products.</summary>
    public const long MaxRequestBodyBytes = 64 * 1024;

    /// <summary>Name of the rate-limiting policy applied to the plan endpoints.</summary>
    public const string PlanRateLimitPolicy = "plan-per-ip";
}

/// <summary>
/// Per-IP fixed-window rate limit for <c>/api/plan</c>. Bound from the <c>RateLimiting</c> configuration section.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const int DefaultPermitLimit = 30;
    public const int DefaultWindowSeconds = 60;

    /// <summary>Requests allowed per client IP per window.</summary>
    public int PermitLimit { get; set; } = DefaultPermitLimit;

    /// <summary>Window length in seconds.</summary>
    public int WindowSeconds { get; set; } = DefaultWindowSeconds;
}

/// <summary>
/// CORS settings bound from the <c>Cors</c> configuration section
/// (environment variables: <c>Cors__AllowedOrigins__0</c>, <c>Cors__AllowedOrigins__1</c>, ...).
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Frontend";

    public string[] AllowedOrigins { get; set; } = [];
}
