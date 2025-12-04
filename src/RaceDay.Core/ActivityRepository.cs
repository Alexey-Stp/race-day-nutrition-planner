namespace RaceDay.Core;

/// <summary>
/// Repository for managing supported activities and their characteristics
/// </summary>
public class ActivityRepository
{
    private static List<ActivityInfo>? _activities;

    /// <summary>
    /// Get all supported activities
    /// </summary>
    public static async Task<List<ActivityInfo>> GetAllActivitiesAsync()
    {
        return await Task.FromResult(_activities ??= new List<ActivityInfo>
        {
            // Sprint Distance Triathlon
            new ActivityInfo(
                Id: "sprint-triathlon",
                Name: "Sprint Triathlon",
                SportType: SportType.Triathlon,
                Description: "Sprint distance triathlon: 750m swim, 20km bike, 5km run",
                MinDurationHours: 1.0,
                MaxDurationHours: 2.5,
                BestTimeHours: 1.25, // 1:15
                BestTimeFormatted: "1:15"
            ),

            // Olympic Distance Triathlon
            new ActivityInfo(
                Id: "olympic-triathlon",
                Name: "Olympic Triathlon",
                SportType: SportType.Triathlon,
                Description: "Olympic distance triathlon: 1.5km swim, 40km bike, 10km run",
                MinDurationHours: 2.0,
                MaxDurationHours: 4.0,
                BestTimeHours: 1.85, // 1:51
                BestTimeFormatted: "1:51"
            ),

            // Half Ironman / 70.3
            new ActivityInfo(
                Id: "half-ironman",
                Name: "Half Ironman (70.3)",
                SportType: SportType.Triathlon,
                Description: "Half Ironman distance: 1.9km swim, 90km bike, 21.1km run",
                MinDurationHours: 4.5,
                MaxDurationHours: 8.0,
                BestTimeHours: 3.75, // 3:45
                BestTimeFormatted: "3:45"
            ),

            // Full Ironman
            new ActivityInfo(
                Id: "ironman",
                Name: "Ironman",
                SportType: SportType.Triathlon,
                Description: "Full Ironman distance: 3.8km swim, 180km bike, 42.2km run",
                MinDurationHours: 10.0,
                MaxDurationHours: 17.0,
                BestTimeHours: 8.0, // 8:00
                BestTimeFormatted: "8:00"
            ),

            // Half Marathon
            new ActivityInfo(
                Id: "half-marathon",
                Name: "Half Marathon",
                SportType: SportType.Run,
                Description: "Running: 21.1km (13.1 miles)",
                MinDurationHours: 1.5,
                MaxDurationHours: 4.0,
                BestTimeHours: 0.583, // 0:35 (35 minutes)
                BestTimeFormatted: "0:35"
            ),

            // Marathon
            new ActivityInfo(
                Id: "marathon",
                Name: "Marathon",
                SportType: SportType.Run,
                Description: "Running: 42.2km (26.2 miles)",
                MinDurationHours: 3.0,
                MaxDurationHours: 7.0,
                BestTimeHours: 2.083, // 2:05
                BestTimeFormatted: "2:05"
            ),

            // Ultramarathon (50km)
            new ActivityInfo(
                Id: "ultramarathon-50k",
                Name: "50K Ultramarathon",
                SportType: SportType.Run,
                Description: "Running: 50km ultramarathon",
                MinDurationHours: 5.0,
                MaxDurationHours: 12.0,
                BestTimeHours: 2.92, // 2:55
                BestTimeFormatted: "2:55"
            ),

            // Road Cycling - Century
            new ActivityInfo(
                Id: "century-ride",
                Name: "Century Ride",
                SportType: SportType.Bike,
                Description: "Cycling: 160km (100 miles)",
                MinDurationHours: 5.0,
                MaxDurationHours: 12.0,
                BestTimeHours: 2.5, // 2:30 (professional pace)
                BestTimeFormatted: "2:30"
            ),

            // Gran Fondo
            new ActivityInfo(
                Id: "gran-fondo",
                Name: "Gran Fondo",
                SportType: SportType.Bike,
                Description: "Cycling: Long distance group ride (typically 120-160km)",
                MinDurationHours: 4.0,
                MaxDurationHours: 10.0,
                BestTimeHours: 2.92, // 2:55
                BestTimeFormatted: "2:55"
            ),

            // Mountain Bike - Marathon MTB
            new ActivityInfo(
                Id: "mtb-marathon",
                Name: "Mountain Bike Marathon",
                SportType: SportType.Bike,
                Description: "Mountain biking: Marathon distance (typically 40-65km)",
                MinDurationHours: 3.5,
                MaxDurationHours: 8.0,
                BestTimeHours: 2.25, // 2:15
                BestTimeFormatted: "2:15"
            ),

            // Gravel Grinder
            new ActivityInfo(
                Id: "gravel-grinder",
                Name: "Gravel Grinder",
                SportType: SportType.Bike,
                Description: "Off-road cycling: Mixed terrain (typically 100-200km)",
                MinDurationHours: 6.0,
                MaxDurationHours: 15.0,
                BestTimeHours: 4.5, // 4:30 (for 160km)
                BestTimeFormatted: "4:30"
            )
        });
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
