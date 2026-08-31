using System.Net;
using System.Net.Http.Json;
using KevTest.Core.Dtos;
using MyMVC.NetApp.Services;
using Xunit;

namespace KevTest.Tests;

public class MvcProductsApiClientTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsProducts()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ProductDto>
            {
                new ProductDto(1, "Widget", 10m),
                new ProductDto(2, "Gadget", 20m)
            })
        });
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        var result = await client.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Widget", result[0].Name);
        Assert.Equal("Gadget", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoProducts()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ProductDto>())
        });
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        var result = await client.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_OnNullResponse()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        });
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        var result = await client.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedProduct()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new ProductDto(1, "New Product", 25m))
        });
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        var result = await client.CreateAsync(new CreateProductDto("New Product", 25m));

        Assert.Equal(1, result.Id);
        Assert.Equal("New Product", result.Name);
        Assert.Equal(25m, result.Price);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOnNonSuccess()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        await Assert.ThrowsAsync<HttpRequestException>(() => 
            client.CreateAsync(new CreateProductDto("Invalid", -5m)));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        var result = await client.DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_OnFailure()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        var result = await client.DeleteAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_SendsCorrectRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new ProductsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost:5001/") });

        await client.DeleteAsync(123);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("123", handler.LastRequest.RequestUri.ToString());
    }
}
