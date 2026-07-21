using System.Net;
using System.Net.Http.Json;
using KevTest.Services;
using Xunit;

namespace KevTest.Tests;

public class ExternalApiClientTests
{
    private record Widget(int Id, string Name);

    [Fact]
    public async Task GetAsync_DeserializesJsonResponse()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new Widget(1, "Sprocket"))
        });
        var client = new ExternalApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        var result = await client.GetAsync<Widget>("widgets/1");

        Assert.NotNull(result);
        Assert.Equal("Sprocket", result!.Name);
    }

    [Fact]
    public async Task PostAsync_SendsBody_AndReturnsDeserializedResponse()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new Widget(2, "Cog"))
        });
        var client = new ExternalApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        var result = await client.PostAsync<Widget, Widget>("widgets", new Widget(0, "Cog"));

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task GetAsync_ThrowsOnNonSuccessStatusCode()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new ExternalApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync<Widget>("widgets/999"));
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new ExternalApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        await client.DeleteAsync("widgets/1");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    }
}
