using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyMVC.NetApp;
using MyMVC.NetApp.Controllers;
using MyMVC.NetApp.Models.Strava;
using MyMVC.NetApp.Services;
using Xunit;

namespace KevTest.Tests;

public class StravaControllerTests
{
    private static StravaController CreateController(
        Mock<IStravaApiClient>? apiClientMock = null,
        Mock<IStravaSyncService>? syncServiceMock = null,
        StravaOptions? options = null,
        string? requestCookieHeader = null)
    {
        apiClientMock ??= new Mock<IStravaApiClient>();
        syncServiceMock ??= new Mock<IStravaSyncService>();
        options ??= new StravaOptions
        {
            ClientId = "12345",
            ClientSecret = "shh",
            RedirectUri = "http://localhost:5000/Strava/Callback",
        };

        var httpContext = new DefaultHttpContext();
        if (requestCookieHeader is not null)
        {
            httpContext.Request.Headers.Append("Cookie", requestCookieHeader);
        }

        var controller = new StravaController(
            apiClientMock.Object,
            syncServiceMock.Object,
            Options.Create(options),
            new Mock<ILogger<StravaController>>().Object,
            new Mock<IStringLocalizer<SharedResource>>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);

        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithStatusAndActivities_FromSyncService()
    {
        var status = new StravaSyncStatus(42, "Kev Roche", "Dublin", "Ireland", null, DateTime.UtcNow, 7);
        var activityPage = new StravaActivityPage(
            new[] { new StravaActivityListItem(1, "Morning Run", "Run", DateTimeOffset.Now, 5000, 1500) }, 1, 100, 7);
        var syncServiceMock = new Mock<IStravaSyncService>();
        syncServiceMock.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(status);
        syncServiceMock.Setup(s => s.GetActivitiesAsync(42, 1, 100, It.IsAny<CancellationToken>())).ReturnsAsync(activityPage);
        var controller = CreateController(syncServiceMock: syncServiceMock);

        var result = await controller.Index(page: 1, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StravaPageViewModel>(viewResult.Model);
        Assert.Same(status, model.Status);
        Assert.Same(activityPage, model.Activities);
    }

    [Fact]
    public async Task Index_RequestsPage1_WhenNotConnected()
    {
        var syncServiceMock = new Mock<IStravaSyncService>();
        syncServiceMock.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync((StravaSyncStatus?)null);
        var controller = CreateController(syncServiceMock: syncServiceMock);

        var result = await controller.Index(page: 1, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StravaPageViewModel>(viewResult.Model);
        Assert.Null(model.Status);
        Assert.Empty(model.Activities.Items);
        syncServiceMock.Verify(s => s.GetActivitiesAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Index_ClampsPageToAtLeastOne_WhenPageIsZeroOrNegative()
    {
        var status = new StravaSyncStatus(42, "Kev Roche", "Dublin", "Ireland", null, DateTime.UtcNow, 7);
        var syncServiceMock = new Mock<IStravaSyncService>();
        syncServiceMock.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(status);
        syncServiceMock.Setup(s => s.GetActivitiesAsync(42, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StravaActivityPage(Array.Empty<StravaActivityListItem>(), 1, 100, 0));
        var controller = CreateController(syncServiceMock: syncServiceMock);

        await controller.Index(page: -5, CancellationToken.None);

        syncServiceMock.Verify(s => s.GetActivitiesAsync(42, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Connect_RedirectsToIndex_WithConfigMissingError_WhenNotConfigured()
    {
        var controller = CreateController(options: new StravaOptions());

        var result = controller.Connect();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("ConfigMissing", controller.TempData["StravaError"]);
    }

    [Fact]
    public void Connect_RedirectsToStravaAuthorizeUrl_AndSetsStateCookie_WhenConfigured()
    {
        var apiClientMock = new Mock<IStravaApiClient>();
        apiClientMock.Setup(c => c.BuildAuthorizeUrl(It.IsAny<string>()))
            .Returns<string>(state => $"https://www.strava.com/oauth/authorize?state={state}");
        var controller = CreateController(apiClientMock: apiClientMock);

        var result = controller.Connect();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("https://www.strava.com/oauth/authorize?state=", redirect.Url);
        Assert.Contains("strava_oauth_state=", controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Callback_RedirectsWithDeniedError_WhenErrorParamPresent()
    {
        var controller = CreateController();

        var result = await controller.Callback(code: null, state: null, error: "access_denied", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Denied", controller.TempData["StravaError"]);
    }

    [Fact]
    public async Task Callback_RedirectsWithInvalidStateError_WhenNoStateCookieWasSet()
    {
        var controller = CreateController();

        var result = await controller.Callback(code: "auth-code", state: "some-state", error: null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("InvalidState", controller.TempData["StravaError"]);
    }

    [Fact]
    public async Task Callback_RedirectsWithInvalidStateError_WhenStateDoesNotMatchCookie()
    {
        var controller = CreateController(requestCookieHeader: "strava_oauth_state=expected-state");

        var result = await controller.Callback(code: "auth-code", state: "different-state", error: null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("InvalidState", controller.TempData["StravaError"]);
    }

    [Fact]
    public async Task Callback_SyncsAndRedirects_WhenCodeAndStateAreValid()
    {
        var syncServiceMock = new Mock<IStravaSyncService>();
        syncServiceMock.Setup(s => s.SyncAsync("auth-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StravaSyncResult("Kev Roche", 12));
        var controller = CreateController(
            syncServiceMock: syncServiceMock,
            requestCookieHeader: "strava_oauth_state=matching-state");

        var result = await controller.Callback(code: "auth-code", state: "matching-state", error: null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Kev Roche|12", controller.TempData["StravaSyncSummary"]);
        syncServiceMock.Verify(s => s.SyncAsync("auth-code", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Callback_RedirectsWithApiError_WhenSyncThrowsHttpRequestException()
    {
        var syncServiceMock = new Mock<IStravaSyncService>();
        syncServiceMock.Setup(s => s.SyncAsync("auth-code", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Strava unavailable"));
        var controller = CreateController(
            syncServiceMock: syncServiceMock,
            requestCookieHeader: "strava_oauth_state=matching-state");

        var result = await controller.Callback(code: "auth-code", state: "matching-state", error: null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApiError", controller.TempData["StravaError"]);
    }
}
