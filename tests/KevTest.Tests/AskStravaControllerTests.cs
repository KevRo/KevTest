using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyMVC.NetApp;
using MyMVC.NetApp.Controllers;
using MyMVC.NetApp.Models.Claude;
using MyMVC.NetApp.Services;
using Xunit;

namespace KevTest.Tests;

public class AskStravaControllerTests
{
    private static AskStravaController CreateController(
        Mock<IAskStravaService>? askStravaServiceMock = null,
        ClaudeOptions? options = null)
    {
        askStravaServiceMock ??= new Mock<IAskStravaService>();
        options ??= new ClaudeOptions { ApiKey = "test-key" };

        var httpContext = new DefaultHttpContext();
        var controller = new AskStravaController(
            askStravaServiceMock.Object,
            Options.Create(options),
            new Mock<ILogger<AskStravaController>>().Object,
            new Mock<IStringLocalizer<SharedResource>>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);

        return controller;
    }

    [Fact]
    public async Task Ask_RedirectsWithConfigMissingError_WhenApiKeyIsBlank()
    {
        var askStravaServiceMock = new Mock<IAskStravaService>();
        var controller = CreateController(askStravaServiceMock, new ClaudeOptions { ApiKey = "" });

        var result = await controller.Ask("How many activities do I have?", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfigMissing", controller.TempData["AskStravaError"]);
        askStravaServiceMock.Verify(
            s => s.AskAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatTurn>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Ask_RedirectsSilently_WhenQuestionIsBlank()
    {
        var controller = CreateController();

        var result = await controller.Ask("   ", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["AskStravaError"]);
    }
}
