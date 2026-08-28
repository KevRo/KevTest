using MyMVC.NetApp.Models.Strava;

namespace MyMVC.NetApp.Services;

public interface IStravaApiClient
{
    string BuildAuthorizeUrl(string state);

    Task<StravaTokenExchangeResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    Task<(StravaAthleteDto Athlete, string RawJson)> GetAthleteAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<string?> GetAthleteStatsRawAsync(string accessToken, long athleteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(StravaActivityDto Activity, string RawJson)>> GetActivitiesPageAsync(
        string accessToken, int page, int perPage, CancellationToken cancellationToken = default);
}
