using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Models.Strava;

namespace MyMVC.NetApp.Services;

public class StravaApiClient : IStravaApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly StravaOptions _options;

    public StravaApiClient(HttpClient httpClient, IOptions<StravaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string BuildAuthorizeUrl(string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["approval_prompt"] = "auto",
            ["scope"] = _options.Scopes,
            ["state"] = state,
        };

        return QueryHelpers.AddQueryString("https://www.strava.com/oauth/authorize", query);
    }

    public async Task<StravaTokenExchangeResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
        };

        var response = await _httpClient.PostAsync("oauth/token", new FormUrlEncodedContent(form), cancellationToken);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var dto = JsonSerializer.Deserialize<StravaTokenExchangeDto>(raw, JsonOptions)
            ?? throw new InvalidOperationException("Strava returned an empty token exchange response.");

        return new StravaTokenExchangeResult(dto, raw);
    }

    public async Task<(StravaAthleteDto Athlete, string RawJson)> GetAthleteAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var raw = await SendAuthorizedGetAsync("api/v3/athlete", accessToken, cancellationToken);
        var dto = JsonSerializer.Deserialize<StravaAthleteDto>(raw, JsonOptions)
            ?? throw new InvalidOperationException("Strava returned an empty athlete response.");

        return (dto, raw);
    }

    public async Task<string?> GetAthleteStatsRawAsync(string accessToken, long athleteId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await SendAuthorizedGetAsync($"api/v3/athletes/{athleteId}/stats", accessToken, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Stats are a nice-to-have; don't fail the whole sync if this one endpoint is unavailable.
            return null;
        }
    }

    public async Task<IReadOnlyList<(StravaActivityDto Activity, string RawJson)>> GetActivitiesPageAsync(
        string accessToken, int page, int perPage, CancellationToken cancellationToken = default)
    {
        var raw = await SendAuthorizedGetAsync($"api/v3/athlete/activities?page={page}&per_page={perPage}", accessToken, cancellationToken);

        using var document = JsonDocument.Parse(raw);
        var results = new List<(StravaActivityDto, string)>();

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var activity = element.Deserialize<StravaActivityDto>(JsonOptions);
            if (activity is not null)
            {
                results.Add((activity, element.GetRawText()));
            }
        }

        return results;
    }

    private async Task<string> SendAuthorizedGetAsync(string requestUri, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
