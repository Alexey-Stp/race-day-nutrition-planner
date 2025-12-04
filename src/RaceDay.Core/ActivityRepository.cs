namespace RaceDay.Core;

using System.Reflection;
using System.Text.Json;

/// <summary>
/// Repository for managing supported activities and their characteristics
/// Loads activities from embedded JSON resource
/// </summary>
public class ActivityRepository
{
    private static List<ActivityInfo>? _activities;

    /// <summary>
    /// Get all supported activities
    /// </summary>
    public static async Task<List<ActivityInfo>> GetAllActivitiesAsync()
    {
        if (_activities != null)
            return _activities;

        _activities = await LoadActivitiesFromJsonAsync();
        return _activities;
    }

    /// <summary>
    /// Load activities from embedded JSON resource
    /// </summary>
    private static async Task<List<ActivityInfo>> LoadActivitiesFromJsonAsync()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "RaceDay.Core.Data.activities.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{resourceName}' not found");

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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
    public static async Task<ActivityInfo?> GetActivityByIdAsync(string id)
    {
        var activities = await GetAllActivitiesAsync();
        return activities.FirstOrDefault(a => a.Id == id);
    }

    /// <summary>
    /// Get activities by sport type
    /// </summary>
    public static async Task<List<ActivityInfo>> GetActivitiesBySportTypeAsync(SportType sportType)
    {
        var activities = await GetAllActivitiesAsync();
        return activities.Where(a => a.SportType == sportType).ToList();
    }

    /// <summary>
    /// Search activities by name or description
    /// </summary>
    public static async Task<List<ActivityInfo>> SearchActivitiesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ActivityInfo>();

        var activities = await GetAllActivitiesAsync();
        var lowerQuery = query.ToLower();

        return activities.Where(a =>
            a.Name.ToLower().Contains(lowerQuery) ||
            a.Description.ToLower().Contains(lowerQuery)
        ).ToList();
    }
}
