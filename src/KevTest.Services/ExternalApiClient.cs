using System.Net.Http.Json;
using KevTest.Core.Interfaces;

namespace KevTest.Services;

/// <summary>
/// Generic JSON HTTP client wrapper for calling external APIs. Register a named/typed
/// HttpClient (base address, auth headers, retry policy, etc.) via IHttpClientFactory
/// and inject that configuration here — callers only deal in request/response types.
/// </summary>
public class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;

    public ExternalApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(requestUri, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(requestUri, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
