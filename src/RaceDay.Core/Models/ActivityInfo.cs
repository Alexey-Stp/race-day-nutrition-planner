namespace RaceDay.Core.Models;

/// <summary>
/// Activity information with time limits and best times
/// </summary>
/// <param name="Id">Unique activity identifier</param>
/// <param name="Name">Display name of the activity</param>
/// <param name="SportType">Associated sport type</param>
/// <param name="Description">Activity description</param>
/// <param name="MinDurationHours">Minimum recommended duration in hours</param>
/// <param name="MaxDurationHours">Maximum typical duration in hours</param>
/// <param name="BestTimeHours">Professional/elite time in hours</param>
public record ActivityInfo(
    string Id,
    string Name,
    SportType SportType,
    string Description,
    double MinDurationHours,
    double MaxDurationHours,
    double BestTimeHours
);
