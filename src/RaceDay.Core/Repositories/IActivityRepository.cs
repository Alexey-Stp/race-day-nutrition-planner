namespace RaceDay.Core.Repositories;

using RaceDay.Core.Models;

/// <summary>
/// Read access to the predefined activity catalogue. Returned lists are read-only snapshots.
/// </summary>
public interface IActivityRepository
{
    /// <summary>Gets all supported activities.</summary>
    /// <exception cref="RaceDay.Core.Exceptions.CatalogueLoadException">The embedded catalogue could not be loaded.</exception>
    Task<IReadOnlyList<ActivityInfo>> GetAllActivitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets an activity by ID (case-insensitive), or null when not found.</summary>
    Task<ActivityInfo?> GetActivityByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Gets the activities for a sport type.</summary>
    Task<IReadOnlyList<ActivityInfo>> GetActivitiesBySportTypeAsync(SportType sportType, CancellationToken cancellationToken = default);

    /// <summary>Searches activities by name or description (case-insensitive). A blank query returns an empty list.</summary>
    Task<IReadOnlyList<ActivityInfo>> SearchActivitiesAsync(string query, CancellationToken cancellationToken = default);
}
