namespace KevTest.Core.Interfaces;

/// <summary>
/// Generic wrapper around outbound HTTP calls to external APIs. Keeps HttpClient
/// and JSON (de)serialization details out of the service layer.
/// </summary>
public interface IExternalApiClient
{
    Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken = default);
    Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
}
