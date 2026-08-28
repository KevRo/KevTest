using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyMVC.NetApp.Data;
using MyMVC.NetApp.Models.Strava;
using MyMVC.NetApp.Services;
using Xunit;

namespace KevTest.Tests;

public class StravaSyncServiceTests
{
    private static StravaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StravaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StravaDbContext(options);
    }

    private static StravaTokenExchangeResult TokenResult(long athleteId = 42) => new(
        new StravaTokenExchangeDto(
            TokenType: "Bearer",
            ExpiresAt: 1700000000,
            ExpiresIn: 21600,
            RefreshToken: "refresh-abc",
            AccessToken: "access-abc",
            Athlete: new StravaAthleteSummaryDto(athleteId, "kev", "Kev", "Roche")),
        RawJson: "{\"access_token\":\"access-abc\"}");

    private static StravaAthleteDto AthleteDto(long id = 42, string first = "Kev", string last = "Roche") => new(
        Id: id, Username: "kev", Firstname: first, Lastname: last, City: "Dublin", State: null, Country: "Ireland",
        Sex: "M", Premium: true, Summit: false, CreatedAt: null, UpdatedAt: null,
        ProfileMedium: "https://example.com/photo.jpg", Profile: "https://example.com/photo-full.jpg",
        FollowerCount: 10, FriendCount: 5, MeasurementPreference: "meters", Ftp: 250, Weight: 70.5);

    private static StravaActivityDto ActivityDto(long id) => new(
        Id: id, Name: $"Activity {id}", Type: "Run", SportType: "Run", Distance: 5000, MovingTime: 1500,
        ElapsedTime: 1600, TotalElevationGain: 30, StartDate: DateTimeOffset.UtcNow, StartDateLocal: DateTimeOffset.Now,
        Timezone: "Europe/Dublin", AverageSpeed: 3.3, MaxSpeed: 5.0, AverageHeartrate: 150, MaxHeartrate: 175,
        Calories: 400, KudosCount: 2, AchievementCount: 1, Trainer: false, Commute: false, Manual: false,
        Private: false, GearId: null);

    private static Mock<IStravaApiClient> CreateApiClientMock(int activitiesPerPage, params int[] countPerPage)
    {
        var mock = new Mock<IStravaApiClient>();
        mock.Setup(c => c.ExchangeCodeForTokenAsync("auth-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenResult());
        mock.Setup(c => c.GetAthleteAsync("access-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AthleteDto(), "{\"id\":42}"));
        mock.Setup(c => c.GetAthleteStatsRawAsync("access-abc", 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"biggest_ride_distance\":50000}");

        // Catch-all: any page not explicitly stubbed below behaves like Strava's real API once
        // activities run out and returns an empty page, so the sync loop stops.
        mock.Setup(c => c.GetActivitiesPageAsync("access-abc", It.IsAny<int>(), activitiesPerPage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<(StravaActivityDto, string)>());

        for (var i = 0; i < countPerPage.Length; i++)
        {
            var page = i + 1;
            var items = Enumerable.Range(1, countPerPage[i])
                .Select(n => (ActivityDto((page * 1000) + n), $"{{\"id\":{(page * 1000) + n}}}"))
                .ToList();
            mock.Setup(c => c.GetActivitiesPageAsync("access-abc", page, activitiesPerPage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);
        }

        return mock;
    }

    private static StravaSyncService CreateService(
        Mock<IStravaApiClient> apiClientMock, StravaDbContext db, StravaOptions? options = null)
        => new(apiClientMock.Object, db, Options.Create(options ?? new StravaOptions
        {
            ActivitiesPageLimit = 5,
            ActivitiesPerPage = 2,
        }), new Mock<ILogger<StravaSyncService>>().Object);

    [Fact]
    public async Task SyncAsync_StoresAthleteTokenAndActivities()
    {
        var apiClientMock = CreateApiClientMock(2, 2);
        using var db = CreateContext();
        var service = CreateService(apiClientMock, db);

        var result = await service.SyncAsync("auth-code");

        Assert.Equal("Kev Roche", result.AthleteName);
        Assert.Equal(2, result.ActivitiesPulled);
        Assert.NotNull(await db.StravaTokens.FindAsync(42L));
        Assert.NotNull(await db.StravaAthletes.FindAsync(42L));
        Assert.Equal(2, await db.StravaActivities.CountAsync());
    }

    [Fact]
    public async Task SyncAsync_UpsertsExistingAthlete_InsteadOfDuplicating()
    {
        var apiClientMock = CreateApiClientMock(2, 1);
        using var db = CreateContext();
        var service = CreateService(apiClientMock, db);

        await service.SyncAsync("auth-code");
        await service.SyncAsync("auth-code");

        Assert.Equal(1, await db.StravaAthletes.CountAsync());
        Assert.Equal(1, await db.StravaTokens.CountAsync());
        Assert.Equal(1, await db.StravaActivities.CountAsync());
    }

    [Fact]
    public async Task SyncAsync_StopsPaging_WhenAPageReturnsFewerThanPerPage()
    {
        // Page 1 full (2 items), page 2 partial (1 item) -> should stop after page 2 without requesting page 3.
        var apiClientMock = CreateApiClientMock(2, 2, 1);
        using var db = CreateContext();
        var service = CreateService(apiClientMock, db);

        var result = await service.SyncAsync("auth-code");

        Assert.Equal(3, result.ActivitiesPulled);
        apiClientMock.Verify(c => c.GetActivitiesPageAsync("access-abc", 3, 2, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_StopsAtPageLimit_EvenWhenMorePagesAreFull()
    {
        var apiClientMock = CreateApiClientMock(2, 2, 2, 2);
        using var db = CreateContext();
        var options = new StravaOptions { ActivitiesPageLimit = 2, ActivitiesPerPage = 2 };
        var service = CreateService(apiClientMock, db, options);

        var result = await service.SyncAsync("auth-code");

        Assert.Equal(4, result.ActivitiesPulled);
        apiClientMock.Verify(c => c.GetActivitiesPageAsync("access-abc", 3, 2, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsNull_WhenNoAthleteStored()
    {
        using var db = CreateContext();
        var service = CreateService(CreateApiClientMock(2), db);

        var status = await service.GetStatusAsync();

        Assert.Null(status);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsSummary_WhenAthleteStored()
    {
        var apiClientMock = CreateApiClientMock(2, 2);
        using var db = CreateContext();
        var service = CreateService(apiClientMock, db);
        await service.SyncAsync("auth-code");

        var status = await service.GetStatusAsync();

        Assert.NotNull(status);
        Assert.Equal(42, status!.AthleteId);
        Assert.Equal("Kev Roche", status.DisplayName);
        Assert.Equal("Dublin", status.City);
        Assert.Equal(2, status.ActivityCount);
    }
}
