using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Data;
using MyMVC.NetApp.Models.Strava;

namespace MyMVC.NetApp.Services;

public class StravaSyncService : IStravaSyncService
{
    private readonly IStravaApiClient _apiClient;
    private readonly StravaDbContext _db;
    private readonly StravaOptions _options;
    private readonly ILogger<StravaSyncService> _logger;

    public StravaSyncService(
        IStravaApiClient apiClient,
        StravaDbContext db,
        IOptions<StravaOptions> options,
        ILogger<StravaSyncService> logger)
    {
        _apiClient = apiClient;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<StravaSyncResult> SyncAsync(string code, CancellationToken cancellationToken = default)
    {
        var tokenResult = await _apiClient.ExchangeCodeForTokenAsync(code, cancellationToken);
        var token = tokenResult.Token;
        var accessToken = token.AccessToken;

        var (athleteDto, athleteRawJson) = await _apiClient.GetAthleteAsync(accessToken, cancellationToken);
        var statsRawJson = await _apiClient.GetAthleteStatsRawAsync(accessToken, athleteDto.Id, cancellationToken);

        var now = DateTime.UtcNow;

        await UpsertTokenAsync(athleteDto.Id, token, now, cancellationToken);
        await UpsertAthleteAsync(athleteDto, athleteRawJson, statsRawJson, now, cancellationToken);
        var activitiesPulled = await PullActivitiesAsync(athleteDto.Id, accessToken, now, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Synced Strava athlete {AthleteId}: {ActivityCount} activities pulled.", athleteDto.Id, activitiesPulled);

        var displayName = $"{athleteDto.Firstname} {athleteDto.Lastname}".Trim();
        return new StravaSyncResult(string.IsNullOrWhiteSpace(displayName) ? "athlete" : displayName, activitiesPulled);
    }

    public async Task<StravaSyncStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var athlete = await _db.StravaAthletes
            .OrderByDescending(a => a.FetchedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (athlete is null)
        {
            return null;
        }

        var activityCount = await _db.StravaActivities.CountAsync(a => a.AthleteId == athlete.Id, cancellationToken);
        var displayName = $"{athlete.Firstname} {athlete.Lastname}".Trim();

        return new StravaSyncStatus(
            athlete.Id,
            string.IsNullOrWhiteSpace(displayName) ? athlete.Username ?? "athlete" : displayName,
            athlete.City,
            athlete.Country,
            athlete.ProfileMediumUrl,
            athlete.FetchedAtUtc,
            activityCount);
    }

    public async Task<StravaActivityPage> GetActivitiesAsync(
        long athleteId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.StravaActivities.Where(a => a.AthleteId == athleteId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var clampedPage = Math.Clamp(page, 1, totalPages);

        var items = await baseQuery
            .OrderByDescending(a => a.StartDateUtc)
            .Skip((clampedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new StravaActivityListItem(a.Id, a.Name, a.Type, a.StartDateLocal, a.Distance, a.MovingTime))
            .ToListAsync(cancellationToken);

        return new StravaActivityPage(items, clampedPage, pageSize, totalCount);
    }

    private async Task UpsertTokenAsync(long athleteId, StravaTokenExchangeDto token, DateTime now, CancellationToken cancellationToken)
    {
        var entity = await _db.StravaTokens.FindAsync(new object[] { athleteId }, cancellationToken);
        if (entity is null)
        {
            entity = new StravaToken { AthleteId = athleteId, CreatedAtUtc = now };
            _db.StravaTokens.Add(entity);
        }

        entity.AccessToken = token.AccessToken;
        entity.RefreshToken = token.RefreshToken;
        entity.ExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(token.ExpiresAt).UtcDateTime;
        entity.TokenType = token.TokenType;
        entity.Scope = _options.Scopes;
        entity.UpdatedAtUtc = now;
    }

    private async Task UpsertAthleteAsync(
        StravaAthleteDto dto, string rawJson, string? statsRawJson, DateTime now, CancellationToken cancellationToken)
    {
        var entity = await _db.StravaAthletes.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (entity is null)
        {
            entity = new StravaAthlete { Id = dto.Id };
            _db.StravaAthletes.Add(entity);
        }

        entity.Username = dto.Username;
        entity.Firstname = dto.Firstname;
        entity.Lastname = dto.Lastname;
        entity.City = dto.City;
        entity.State = dto.State;
        entity.Country = dto.Country;
        entity.Sex = dto.Sex;
        entity.Premium = dto.Premium;
        entity.Summit = dto.Summit;
        entity.StravaCreatedAt = dto.CreatedAt;
        entity.StravaUpdatedAt = dto.UpdatedAt;
        entity.ProfileMediumUrl = dto.ProfileMedium;
        entity.ProfileUrl = dto.Profile;
        entity.FollowerCount = dto.FollowerCount;
        entity.FriendCount = dto.FriendCount;
        entity.MeasurementPreference = dto.MeasurementPreference;
        entity.Ftp = dto.Ftp;
        entity.Weight = dto.Weight;
        entity.ProfileRawJson = rawJson;
        entity.StatsRawJson = statsRawJson;
        entity.FetchedAtUtc = now;
    }

    private async Task<int> PullActivitiesAsync(long athleteId, string accessToken, DateTime now, CancellationToken cancellationToken)
    {
        var activitiesPulled = 0;

        for (var page = 1; page <= _options.ActivitiesPageLimit; page++)
        {
            var activities = await _apiClient.GetActivitiesPageAsync(accessToken, page, _options.ActivitiesPerPage, cancellationToken);
            if (activities.Count == 0)
            {
                break;
            }

            foreach (var (dto, rawJson) in activities)
            {
                var entity = await _db.StravaActivities.FindAsync(new object[] { dto.Id }, cancellationToken);
                if (entity is null)
                {
                    entity = new StravaActivity { Id = dto.Id };
                    _db.StravaActivities.Add(entity);
                }

                entity.AthleteId = athleteId;
                entity.Name = dto.Name;
                entity.Type = dto.Type;
                entity.SportType = dto.SportType;
                entity.Distance = dto.Distance;
                entity.MovingTime = dto.MovingTime;
                entity.ElapsedTime = dto.ElapsedTime;
                entity.TotalElevationGain = dto.TotalElevationGain;
                entity.StartDateUtc = dto.StartDate.UtcDateTime;
                entity.StartDateLocal = dto.StartDateLocal;
                entity.Timezone = dto.Timezone;
                entity.AverageSpeed = dto.AverageSpeed;
                entity.MaxSpeed = dto.MaxSpeed;
                entity.AverageHeartrate = dto.AverageHeartrate;
                entity.MaxHeartrate = dto.MaxHeartrate;
                entity.Calories = dto.Calories;
                entity.KudosCount = dto.KudosCount;
                entity.AchievementCount = dto.AchievementCount;
                entity.Trainer = dto.Trainer;
                entity.Commute = dto.Commute;
                entity.Manual = dto.Manual;
                entity.Private = dto.Private;
                entity.GearId = dto.GearId;
                entity.RawJson = rawJson;
                entity.FetchedAtUtc = now;

                activitiesPulled++;
            }

            if (activities.Count < _options.ActivitiesPerPage)
            {
                break;
            }
        }

        return activitiesPulled;
    }
}
