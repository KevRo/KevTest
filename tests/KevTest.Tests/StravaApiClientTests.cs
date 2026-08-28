using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Models.Strava;
using MyMVC.NetApp.Services;
using Xunit;

namespace KevTest.Tests;

public class StravaApiClientTests
{
    private static StravaOptions DefaultOptions() => new()
    {
        ClientId = "12345",
        ClientSecret = "shh",
        RedirectUri = "http://localhost:5000/Strava/Callback",
        Scopes = "read,activity:read_all,profile:read_all",
    };

    private static StravaApiClient CreateClient(FakeHttpMessageHandler handler, StravaOptions? options = null)
        => new(new HttpClient(handler) { BaseAddress = new Uri("https://www.strava.com/") }, Options.Create(options ?? DefaultOptions()));

    [Fact]
    public void BuildAuthorizeUrl_IncludesClientIdRedirectUriScopeAndState()
    {
        var client = CreateClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var url = client.BuildAuthorizeUrl("state123");

        Assert.StartsWith("https://www.strava.com/oauth/authorize?", url);
        Assert.Contains("client_id=12345", url);
        Assert.Contains("state=state123", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost%3A5000%2FStrava%2FCallback", url);
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_ParsesTokenAndAthlete_AndKeepsRawJson()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                token_type = "Bearer",
                expires_at = 1700000000,
                expires_in = 21600,
                refresh_token = "refresh-abc",
                access_token = "access-abc",
                athlete = new { id = 42, username = "kev", firstname = "Kev", lastname = "Roche" },
            }),
        });
        var client = CreateClient(handler);

        var result = await client.ExchangeCodeForTokenAsync("auth-code");

        Assert.Equal("access-abc", result.Token.AccessToken);
        Assert.Equal("refresh-abc", result.Token.RefreshToken);
        Assert.Equal(42, result.Token.Athlete!.Id);
        Assert.Contains("access-abc", result.RawJson);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/oauth/token", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetAthleteAsync_ParsesProfile_AndSendsBearerToken()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = 42,
                username = "kev",
                firstname = "Kev",
                lastname = "Roche",
                city = "Dublin",
                country = "Ireland",
                premium = true,
            }),
        });
        var client = CreateClient(handler);

        var (athlete, rawJson) = await client.GetAthleteAsync("access-abc");

        Assert.Equal(42, athlete.Id);
        Assert.Equal("Dublin", athlete.City);
        Assert.True(athlete.Premium);
        Assert.Contains("\"city\":\"Dublin\"", rawJson);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("access-abc", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal("/api/v3/athlete", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetAthleteStatsRawAsync_ReturnsNull_WhenApiCallFails()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(handler);

        var stats = await client.GetAthleteStatsRawAsync("access-abc", 42);

        Assert.Null(stats);
    }

    [Fact]
    public async Task GetAthleteStatsRawAsync_ReturnsRawJson_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"biggest_ride_distance\":50000}"),
        });
        var client = CreateClient(handler);

        var stats = await client.GetAthleteStatsRawAsync("access-abc", 42);

        Assert.Equal("{\"biggest_ride_distance\":50000}", stats);
        Assert.Equal("/api/v3/athletes/42/stats", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetActivitiesPageAsync_ParsesEachActivity_AndKeepsPerActivityRawJson()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[]
            {
                new { id = 1, name = "Morning Run", type = "Run", distance = 5000.0, moving_time = 1500, elapsed_time = 1600, total_elevation_gain = 30.0, start_date = "2026-01-01T06:00:00Z", start_date_local = "2026-01-01T06:00:00Z" },
                new { id = 2, name = "Evening Ride", type = "Ride", distance = 20000.0, moving_time = 3000, elapsed_time = 3100, total_elevation_gain = 100.0, start_date = "2026-01-02T18:00:00Z", start_date_local = "2026-01-02T18:00:00Z" },
            }),
        });
        var client = CreateClient(handler);

        var activities = await client.GetActivitiesPageAsync("access-abc", 1, 200);

        Assert.Equal(2, activities.Count);
        Assert.Equal("Morning Run", activities[0].Activity.Name);
        Assert.Equal(1, activities[0].Activity.Id);
        Assert.Contains("\"name\":\"Morning Run\"", activities[0].RawJson);
        Assert.Equal("Evening Ride", activities[1].Activity.Name);
        Assert.Equal("/api/v3/athlete/activities", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("page=1", handler.LastRequest.RequestUri.Query);
        Assert.Contains("per_page=200", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async Task GetActivitiesPageAsync_ReturnsEmptyList_WhenNoActivities()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<object>()),
        });
        var client = CreateClient(handler);

        var activities = await client.GetActivitiesPageAsync("access-abc", 3, 200);

        Assert.Empty(activities);
    }
}
