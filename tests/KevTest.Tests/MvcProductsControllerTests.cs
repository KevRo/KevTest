using KevTest.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using MyMVC.NetApp;
using MyMVC.NetApp.Controllers;
using MyMVC.NetApp.Services;
using System.Collections.Generic;
using Xunit;

namespace KevTest.Tests;

public class MvcProductsControllerTests
{
    private static ProductsController CreateController(
        Mock<IProductsApiClient>? apiClientMock = null,
        Mock<ILogger<ProductsController>>? loggerMock = null,
        Mock<IStringLocalizer<SharedResource>>? localizerMock = null)
    {
        apiClientMock ??= new Mock<IProductsApiClient>();
        loggerMock ??= new Mock<ILogger<ProductsController>>();
        localizerMock ??= new Mock<IStringLocalizer<SharedResource>>();

        return new ProductsController(
            apiClientMock.Object,
            loggerMock.Object,
            localizerMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewWithProducts_WhenApiCallSucceeds()
    {
        var products = new List<ProductDto>
        {
            new ProductDto(1, "Widget", 10m),
            new ProductDto(2, "Gadget", 20m)
        };
        var apiClientMock = new Mock<IProductsApiClient>();
        apiClientMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var controller = CreateController(apiClientMock: apiClientMock);

        var result = await controller.Index(CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ProductDto>>(viewResult.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Index_ReturnsViewWithEmptyList_WhenApiCallFails()
    {
        var apiClientMock = new Mock<IProductsApiClient>();
        apiClientMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var loggerMock = new Mock<ILogger<ProductsController>>();
        var controller = CreateController(apiClientMock: apiClientMock, loggerMock: loggerMock);

        var result = await controller.Index(CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ProductDto>>(viewResult.Model);
        Assert.Empty(model);
        Assert.True(controller.ViewData.ContainsKey("ApiError"));
        Assert.True((bool)controller.ViewData["ApiError"]!);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not reach the Products API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Create_Get_ReturnsViewWithEmptyDto()
    {
        var controller = CreateController();

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CreateProductDto>(viewResult.Model);
        Assert.Equal(string.Empty, model.Name);
        Assert.Equal(0, model.Price);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithError_WhenNameIsEmpty()
    {
        var localizerMock = new Mock<IStringLocalizer<SharedResource>>();
        localizerMock.Setup(x => x["Products_Create_NameRequired"])
            .Returns(new LocalizedString("Products_Create_NameRequired", "Name is required"));

        var controller = CreateController(localizerMock: localizerMock);
        controller.ModelState.Clear();

        var result = await controller.Create(new CreateProductDto("", 10m), CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(CreateProductDto.Name)));
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithError_WhenPriceIsInvalid()
    {
        var localizerMock = new Mock<IStringLocalizer<SharedResource>>();
        localizerMock.Setup(x => x["Products_Create_PriceInvalid"])
            .Returns(new LocalizedString("Products_Create_PriceInvalid", "Price must be positive"));

        var controller = CreateController(localizerMock: localizerMock);
        controller.ModelState.Clear();

        var result = await controller.Create(new CreateProductDto("Test", -5m), CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(CreateProductDto.Price)));
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithError_WhenNameIsWhitespace()
    {
        var localizerMock = new Mock<IStringLocalizer<SharedResource>>();
        localizerMock.Setup(x => x["Products_Create_NameRequired"])
            .Returns(new LocalizedString("Products_Create_NameRequired", "Name is required"));

        var controller = CreateController(localizerMock: localizerMock);
        controller.ModelState.Clear();

        var result = await controller.Create(new CreateProductDto("   ", 10m), CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_Post_CallsApiAndRedirects_WhenValid()
    {
        var apiClientMock = new Mock<IProductsApiClient>();
        apiClientMock.Setup(x => x.CreateAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(1, "Test", 10m));

        var controller = CreateController(apiClientMock: apiClientMock);
        controller.ModelState.Clear();

        var result = await controller.Create(new CreateProductDto("Test", 10m), CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        apiClientMock.Verify(
            x => x.CreateAsync(It.Is<CreateProductDto>(d => d.Name == "Test" && d.Price == 10m), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_CallsApiAndRedirects()
    {
        var apiClientMock = new Mock<IProductsApiClient>();
        apiClientMock.Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController(apiClientMock: apiClientMock);

        var result = await controller.Delete(123, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        apiClientMock.Verify(
            x => x.DeleteAsync(123, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_CallsApiEvenWhenApiReturnsFalse()
    {
        var apiClientMock = new Mock<IProductsApiClient>();
        apiClientMock.Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController(apiClientMock: apiClientMock);

        var result = await controller.Delete(999, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        apiClientMock.Verify(
            x => x.DeleteAsync(999, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
