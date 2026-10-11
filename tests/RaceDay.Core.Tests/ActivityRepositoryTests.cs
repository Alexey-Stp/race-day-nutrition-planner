namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;
using RaceDay.Core.Exceptions;

public class ActivityRepositoryTests
{
    private readonly ActivityRepository _repository = new();

    #region GetAllActivitiesAsync Tests

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsListOfActivities()
    {
        // Act
        var activities = await _repository.GetAllActivitiesAsync();

        // Assert
        activities.ShouldNotBeNull();
        activities.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_WithCancellationToken_ReturnsListOfActivities()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var activities = await _repository.GetAllActivitiesAsync(cts.Token);

        // Assert
        activities.ShouldNotBeNull();
        activities.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsActivitiesWithValidData()
    {
        // Act
        var activities = await _repository.GetAllActivitiesAsync();

        // Assert
        foreach (var activity in activities)
        {
            (string.IsNullOrWhiteSpace(activity.Id)).ShouldBeFalse();
            (string.IsNullOrWhiteSpace(activity.Name)).ShouldBeFalse();
            (string.IsNullOrWhiteSpace(activity.Description)).ShouldBeFalse();
            activity.MinDurationHours.ShouldNotBe(0);
            activity.MaxDurationHours.ShouldNotBe(0);
            (activity.MinDurationHours <= activity.MaxDurationHours).ShouldBeTrue(
                "MinDuration should be <= MaxDuration");
        }
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnsCachedResult()
    {
        // Act
        var first = await _repository.GetAllActivitiesAsync();
        var second = await _repository.GetAllActivitiesAsync();

        // Assert - should return the same cached instance
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ContainsKnownActivities()
    {
        // Act
        var activities = await _repository.GetAllActivitiesAsync();
        var activityIds = activities.Select(a => a.Id).ToList();

        // Assert - verify some known activities exist
        activityIds.ShouldContain("run");
        activityIds.ShouldContain("bike");
        activityIds.ShouldContain("triathlon");
    }

    [Fact]
    public async Task GetAllActivitiesAsync_AllActivitiesHaveUniqueIds()
    {
        // Act
        var activities = await _repository.GetAllActivitiesAsync();

        // Assert
        var ids = activities.Select(a => a.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        uniqueIds.Count.ShouldBe(ids.Count);
    }

    [Fact]
    public async Task GetAllActivitiesAsync_AllActivitiesHaveSportType()
    {
        // Act
        var activities = await _repository.GetAllActivitiesAsync();

        // Assert
        foreach (var activity in activities)
        {
            var isValidSportType = Enum.IsDefined(typeof(SportType), activity.SportType);
            (isValidSportType).ShouldBeTrue($"Invalid SportType: {activity.SportType}");
        }
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ContainsMultipleSportTypes()
    {
        // Act
        var activities = await _repository.GetAllActivitiesAsync();
        var sportTypes = activities.Select(a => a.SportType).Distinct().ToList();

        // Assert - should have at least 2 different sport types
        (sportTypes.Count >= 2).ShouldBeTrue(
            $"Expected at least 2 sport types, but found {sportTypes.Count}");
    }

    #endregion

    #region GetActivityByIdAsync Tests

    [Fact]
    public async Task GetActivityByIdAsync_WithValidId_ReturnsActivity()
    {
        // Act
        var activity = await _repository.GetActivityByIdAsync("run");

        // Assert
        activity.ShouldNotBeNull();
        activity.Id.ShouldBe("run");
    }

    [Fact]
    public async Task GetActivityByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var activity = await _repository.GetActivityByIdAsync("nonexistent-activity");

        // Assert
        activity.ShouldBeNull();
    }

    [Fact]
    public async Task GetActivityByIdAsync_DifferentCase_ReturnsSameActivity()
    {
        // ID lookups are case-insensitive (OrdinalIgnoreCase), matching the product repository.
        // Act
        var lowerCase = await _repository.GetActivityByIdAsync("run");
        var upperCase = await _repository.GetActivityByIdAsync("RUN");

        // Assert
        lowerCase.ShouldNotBeNull();
        upperCase.ShouldBeSameAs(lowerCase);
    }

    #endregion

    #region GetActivitiesBySportTypeAsync Tests

    [Fact]
    public async Task GetActivitiesBySportTypeAsync_WithValidType_ReturnsActivities()
    {
        // Act
        var activities = await _repository.GetActivitiesBySportTypeAsync(SportType.Run);

        // Assert
        activities.ShouldNotBeNull();
        activities.ShouldNotBeEmpty();
        Assert.All(activities, a => Assert.Equal(SportType.Run, a.SportType));
    }

    [Fact]
    public async Task GetActivitiesBySportTypeAsync_WithInvalidType_ReturnsEmpty()
    {
        // Act
        var activities = await _repository.GetActivitiesBySportTypeAsync(
            (SportType)999); // Invalid sport type

        // Assert
        activities.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetActivitiesBySportTypeAsync_ContainsOnlyRequestedType()
    {
        // Act
        var triActivities = await _repository.GetActivitiesBySportTypeAsync(SportType.Triathlon);

        // Assert
        Assert.All(triActivities, a => Assert.Equal(SportType.Triathlon, a.SportType));
    }

    #endregion

    #region SearchActivitiesAsync Tests

    [Fact]
    public async Task SearchActivitiesAsync_WithValidQuery_ReturnsMatchingActivities()
    {
        // Act
        var results = await _repository.SearchActivitiesAsync("run");

        // Assert
        results.ShouldNotBeNull();
        results.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SearchActivitiesAsync_WithInvalidQuery_ReturnsEmpty()
    {
        // Act
        var results = await _repository.SearchActivitiesAsync("nonexistentactivity123");

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchActivitiesAsync_IsCase_Insensitive()
    {
        // Act
        var lowerCase = await _repository.SearchActivitiesAsync("run");
        var upperCase = await _repository.SearchActivitiesAsync("RUN");
        var mixedCase = await _repository.SearchActivitiesAsync("RuN");

        // Assert
        upperCase.Count.ShouldBe(lowerCase.Count);
        mixedCase.Count.ShouldBe(lowerCase.Count);
        Assert.All(new[] { lowerCase, upperCase, mixedCase },
            results => results.ShouldNotBeEmpty());
    }

    #endregion

    #region Review findings: culture, immutability, load failure

    [Fact]
    public async Task SearchActivitiesAsync_TurkishCulture_MatchesDottedI()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            // Under tr-TR "TRIATHLON".ToLower() is "trıathlon" (dotless ı), which used to match nothing.
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");

            var results = await _repository.SearchActivitiesAsync("TRIATHLON");

            results.ShouldNotBeEmpty();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetActivityByIdAsync_NullOrEmptyId_ReturnsNull(string? id)
    {
        (await _repository.GetActivityByIdAsync(id!)).ShouldBeNull();
    }

    [Fact]
    public async Task GetAllActivitiesAsync_ReturnedList_CannotCorruptCache()
    {
        var activities = await _repository.GetAllActivitiesAsync();
        int count = activities.Count;

        if (activities is IList<ActivityInfo> list)
        {
            Should.Throw<NotSupportedException>(() => list.Clear());
            Should.Throw<NotSupportedException>(() => list.Add(list[0]));
        }

        (await _repository.GetAllActivitiesAsync()).Count.ShouldBe(count);
    }

    [Fact]
    public async Task GetActivitiesBySportTypeAsync_ReturnedList_IsIndependentOfCache()
    {
        var all = await _repository.GetAllActivitiesAsync();
        var runs = await _repository.GetActivitiesBySportTypeAsync(SportType.Run);

        runs.ShouldNotBeSameAs(all);
        (await _repository.GetAllActivitiesAsync()).Count.ShouldBe(all.Count);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("null")]
    public void ParseActivities_InvalidCatalogue_ThrowsCatalogueLoadException(string json)
    {
        Should.Throw<CatalogueLoadException>(() => ActivityRepository.ParseActivities(json));
    }

    #endregion
}
