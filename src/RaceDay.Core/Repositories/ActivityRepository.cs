namespace RaceDay.Core.Repositories;

using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;

/// <summary>
/// Repository for managing supported activities and their characteristics.
/// Loads activities from an embedded JSON resource once per process (thread-safe) and hands out a read-only view.
/// </summary>
public sealed class ActivityRepository : IActivityRepository
{
    private const string CatalogueName = "activity";
    private const string ResourceName = "RaceDay.Core.Data.activities.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // PublicationOnly: a failed load is not cached, so a later call can retry.
    private static readonly Lazy<IReadOnlyList<ActivityInfo>> Activities =
        new(LoadEmbeddedActivities, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Get all supported activities. The same read-only instance is returned on every call.
    /// </summary>
    /// <exception cref="CatalogueLoadException">The embedded catalogue could not be loaded.</exception>
    public Task<IReadOnlyList<ActivityInfo>> GetAllActivitiesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Activities.Value);
    }

    /// <summary>
    /// Get activity by ID (case-insensitive)
    /// </summary>
    public async Task<ActivityInfo?> GetActivityByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var activities = await GetAllActivitiesAsync(cancellationToken);
        return activities.FirstOrDefault(a => a.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get activities by sport type
    /// </summary>
    public async Task<IReadOnlyList<ActivityInfo>> GetActivitiesBySportTypeAsync(SportType sportType, CancellationToken cancellationToken = default)
    {
        var activities = await GetAllActivitiesAsync(cancellationToken);
        return activities.Where(a => a.SportType == sportType).ToList();
    }

    /// <summary>
    /// Search activities by name or description (case-insensitive, culture-independent)
    /// </summary>
    public async Task<IReadOnlyList<ActivityInfo>> SearchActivitiesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<ActivityInfo>();

        var term = query.Trim();
        var activities = await GetAllActivitiesAsync(cancellationToken);
        return activities.Where(a =>
                a.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Parses an activity catalogue. Exposed internally so malformed input can be tested.
    /// </summary>
    /// <exception cref="CatalogueLoadException">The JSON is malformed or contains no activities.</exception>
    internal static IReadOnlyList<ActivityInfo> ParseActivities(string json)
    {
        try
        {
            var activities = JsonSerializer.Deserialize<List<ActivityInfo>>(json, JsonOptions);
            if (activities is not { Count: > 0 })
                throw new CatalogueLoadException(CatalogueName, "the catalogue contains no activities");
            return activities.AsReadOnly();
        }
        catch (JsonException ex)
        {
            throw new CatalogueLoadException(CatalogueName, ex.Message, ex);
        }
    }

    private static IReadOnlyList<ActivityInfo> LoadEmbeddedActivities()
    {
        using var stream = typeof(ActivityRepository).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new CatalogueLoadException(CatalogueName, $"embedded resource '{ResourceName}' not found");
        using var reader = new StreamReader(stream);
        return ParseActivities(reader.ReadToEnd());
    }
}
