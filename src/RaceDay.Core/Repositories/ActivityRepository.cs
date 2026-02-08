namespace RaceDay.Core.Repositories;

using System.Reflection;
using System.Text.Json;
using RaceDay.Core.Models;

/// <summary>
/// Repository for managing supported activities and their characteristics
/// Loads activities from embedded JSON resource
/// </summary>
public static class ActivityRepository
{
    private static List<ActivityInfo>? _activities;

    /// <summary>
    /// Get all supported activities
    /// </summary>
    public static async Task<List<ActivityInfo>> GetAllActivitiesAsync(CancellationToken cancellationToken = default)
    {
        if (_activities != null)
            return _activities;

        _activities = await LoadActivitiesFromJsonAsync(cancellationToken);
        return _activities;
    }

    /// <summary>
    /// Load activities from embedded JSON resource
    /// </summary>
    private static async Task<List<ActivityInfo>> LoadActivitiesFromJsonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var assembly = typeof(ActivityRepository).Assembly;
            var resourceName = "RaceDay.Core.Data.activities.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{resourceName}' not found");

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(cancellationToken);

            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var activities = JsonSerializer.Deserialize<List<ActivityInfo>>(json, options);

            return activities ?? new List<ActivityInfo>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load activities from JSON resource", ex);
        }
    }

    /// <summary>
    /// Get activity by ID
    /// </summary>
    public static async Task<ActivityInfo?> GetActivityByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var activities = await GetAllActivitiesAsync(cancellationToken);
        return activities.FirstOrDefault(a => a.Id == id);
    }

    /// <summary>
    /// Get activities by sport type
    /// </summary>
    public static async Task<List<ActivityInfo>> GetActivitiesBySportTypeAsync(SportType sportType, CancellationToken cancellationToken = default)
    {
        var activities = await GetAllActivitiesAsync(cancellationToken);
        return activities.Where(a => a.SportType == sportType).ToList();
    }

    /// <summary>
    /// Search activities by name or description
    /// </summary>
    public static async Task<List<ActivityInfo>> SearchActivitiesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ActivityInfo>();

        var activities = await GetAllActivitiesAsync(cancellationToken);
        var lowerQuery = query.ToLower();

        return activities.Where(a =>
            a.Name.ToLower().Contains(lowerQuery) ||
            a.Description.ToLower().Contains(lowerQuery)
        ).ToList();
    }
}
