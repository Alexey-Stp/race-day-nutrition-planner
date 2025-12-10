namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;

public class ActivityRepositoryTests
{
    #region GetAllActivitiesAsync Tests

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsListOfActivities()
    {
        // Act
        var activities = await ActivityRepository.GetAllActivitiesAsync();

        // Assert
        Assert.NotNull(activities);
        Assert.NotEmpty(activities);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WithCancellationToken_ReturnsListOfActivities()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var activities = await ActivityRepository.GetAllActivitiesAsync(cts.Token);

        // Assert
        Assert.NotNull(activities);
        Assert.NotEmpty(activities);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsActivitiesWithValidData()
    {
        // Act
        var activities = await ActivityRepository.GetAllActivitiesAsync();

        // Assert
        foreach (var activity in activities)
        {
            Assert.False(string.IsNullOrWhiteSpace(activity.Id));
            Assert.False(string.IsNullOrWhiteSpace(activity.Name));
            Assert.False(string.IsNullOrWhiteSpace(activity.Description));
            Assert.NotEqual(0, activity.MinDurationHours);
            Assert.NotEqual(0, activity.MaxDurationHours);
            Assert.True(activity.MinDurationHours <= activity.MaxDurationHours, 
                "MinDuration should be <= MaxDuration");
        }
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsCachedResult()
    {
        // Act
        var first = await ActivityRepository.GetAllActivitiesAsync();
        var second = await ActivityRepository.GetAllActivitiesAsync();

        // Assert - should return the same cached instance
        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ContainsKnownActivities()
    {
        // Act
        var activities = await ActivityRepository.GetAllActivitiesAsync();
        var activityIds = activities.Select(a => a.Id).ToList();

        // Assert - verify some known activities exist
        Assert.Contains("run", activityIds);
        Assert.Contains("bike", activityIds);
        Assert.Contains("triathlon", activityIds);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_AllActivitiesHaveUniqueIds()
    {
        // Act
        var activities = await ActivityRepository.GetAllActivitiesAsync();

        // Assert
        var ids = activities.Select(a => a.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        Assert.Equal(ids.Count, uniqueIds.Count);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_AllActivitiesHaveSportType()
    {
        // Act
        var activities = await ActivityRepository.GetAllActivitiesAsync();

        // Assert
        foreach (var activity in activities)
        {
            var isValidSportType = Enum.IsDefined(typeof(SportType), activity.SportType);
            Assert.True(isValidSportType, $"Invalid SportType: {activity.SportType}");
        }
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ContainsMultipleSportTypes()
    {
        // Act
        var activities = await ActivityRepository.GetAllActivitiesAsync();
        var sportTypes = activities.Select(a => a.SportType).Distinct().ToList();

        // Assert - should have at least 2 different sport types
        Assert.True(sportTypes.Count >= 2, 
            $"Expected at least 2 sport types, but found {sportTypes.Count}");
    }

    #endregion

    #region GetActivityByIdAsync Tests

    [Fact]
    public async Task GetActivityByIdAsync_WithValidId_ReturnsActivity()
    {
        // Act
        var activity = await ActivityRepository.GetActivityByIdAsync("run");

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("run", activity.Id);
    }

    [Fact]
    public async Task GetActivityByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var activity = await ActivityRepository.GetActivityByIdAsync("nonexistent-activity");

        // Assert
        Assert.Null(activity);
    }

    [Fact]
    public async Task GetActivityByIdAsync_IsCase_Sensitive()
    {
        // Act
        var lowerCase = await ActivityRepository.GetActivityByIdAsync("run");
        var upperCase = await ActivityRepository.GetActivityByIdAsync("RUN");

        // Assert
        Assert.NotNull(lowerCase);
        Assert.Null(upperCase);
    }

    #endregion

    #region GetActivitiesBySportTypeAsync Tests

    [Fact]
    public async Task GetActivitiesBySportTypeAsync_WithValidType_ReturnsActivities()
    {
        // Act
        var activities = await ActivityRepository.GetActivitiesBySportTypeAsync(SportType.Run);

        // Assert
        Assert.NotNull(activities);
        Assert.NotEmpty(activities);
        Assert.All(activities, a => Assert.Equal(SportType.Run, a.SportType));
    }

    [Fact]
    public async Task GetActivitiesBySportTypeAsync_WithInvalidType_ReturnsEmpty()
    {
        // Act
        var activities = await ActivityRepository.GetActivitiesBySportTypeAsync(
            (SportType)999); // Invalid sport type

        // Assert
        Assert.Empty(activities);
    }

    [Fact]
    public async Task GetActivitiesBySportTypeAsync_ContainsOnlyRequestedType()
    {
        // Act
        var triActivities = await ActivityRepository.GetActivitiesBySportTypeAsync(SportType.Triathlon);

        // Assert
        Assert.All(triActivities, a => Assert.Equal(SportType.Triathlon, a.SportType));
    }

    #endregion

    #region SearchActivitiesAsync Tests

    [Fact]
    public async Task SearchActivitiesAsync_WithValidQuery_ReturnsMatchingActivities()
    {
        // Act
        var results = await ActivityRepository.SearchActivitiesAsync("run");

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task SearchActivitiesAsync_WithInvalidQuery_ReturnsEmpty()
    {
        // Act
        var results = await ActivityRepository.SearchActivitiesAsync("nonexistentactivity123");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchActivitiesAsync_IsCase_Insensitive()
    {
        // Act
        var lowerCase = await ActivityRepository.SearchActivitiesAsync("run");
        var upperCase = await ActivityRepository.SearchActivitiesAsync("RUN");
        var mixedCase = await ActivityRepository.SearchActivitiesAsync("RuN");

        // Assert
        Assert.Equal(lowerCase.Count, upperCase.Count);
        Assert.Equal(lowerCase.Count, mixedCase.Count);
        Assert.All(new[] { lowerCase, upperCase, mixedCase }, 
            results => Assert.NotEmpty(results));
    }

    #endregion
}
