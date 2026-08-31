using System.Text.Json.Serialization;

namespace MyMVC.NetApp.Models.Strava;

public record StravaTokenExchangeDto(
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_at")] long ExpiresAt,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("athlete")] StravaAthleteSummaryDto? Athlete);

public record StravaAthleteSummaryDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("firstname")] string? Firstname,
    [property: JsonPropertyName("lastname")] string? Lastname);

public record StravaTokenExchangeResult(StravaTokenExchangeDto Token, string RawJson);

public record StravaAthleteDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("firstname")] string? Firstname,
    [property: JsonPropertyName("lastname")] string? Lastname,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("sex")] string? Sex,
    [property: JsonPropertyName("premium")] bool Premium,
    [property: JsonPropertyName("summit")] bool Summit,
    [property: JsonPropertyName("created_at")] DateTimeOffset? CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt,
    [property: JsonPropertyName("profile_medium")] string? ProfileMedium,
    [property: JsonPropertyName("profile")] string? Profile,
    [property: JsonPropertyName("follower_count")] int? FollowerCount,
    [property: JsonPropertyName("friend_count")] int? FriendCount,
    [property: JsonPropertyName("measurement_preference")] string? MeasurementPreference,
    [property: JsonPropertyName("ftp")] int? Ftp,
    [property: JsonPropertyName("weight")] double? Weight);

public record StravaActivityDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("sport_type")] string? SportType,
    [property: JsonPropertyName("distance")] double Distance,
    [property: JsonPropertyName("moving_time")] int MovingTime,
    [property: JsonPropertyName("elapsed_time")] int ElapsedTime,
    [property: JsonPropertyName("total_elevation_gain")] double TotalElevationGain,
    [property: JsonPropertyName("start_date")] DateTimeOffset StartDate,
    [property: JsonPropertyName("start_date_local")] DateTimeOffset StartDateLocal,
    [property: JsonPropertyName("timezone")] string? Timezone,
    [property: JsonPropertyName("average_speed")] double? AverageSpeed,
    [property: JsonPropertyName("max_speed")] double? MaxSpeed,
    [property: JsonPropertyName("average_heartrate")] double? AverageHeartrate,
    [property: JsonPropertyName("max_heartrate")] double? MaxHeartrate,
    [property: JsonPropertyName("calories")] double? Calories,
    [property: JsonPropertyName("kudos_count")] int KudosCount,
    [property: JsonPropertyName("achievement_count")] int AchievementCount,
    [property: JsonPropertyName("trainer")] bool Trainer,
    [property: JsonPropertyName("commute")] bool Commute,
    [property: JsonPropertyName("manual")] bool Manual,
    [property: JsonPropertyName("private")] bool Private,
    [property: JsonPropertyName("gear_id")] string? GearId);

public record StravaSyncResult(string AthleteName, int ActivitiesPulled);

public record StravaSyncStatus(
    long AthleteId,
    string DisplayName,
    string? City,
    string? Country,
    string? ProfileImageUrl,
    DateTime LastSyncedUtc,
    int ActivityCount,
    double TotalDistanceMeters);

public record StravaActivityListItem(
    long Id,
    string? Name,
    string? Type,
    DateTimeOffset StartDateLocal,
    double Distance,
    int MovingTime);

public record StravaActivityPage(IReadOnlyList<StravaActivityListItem> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record StravaPageViewModel(StravaSyncStatus? Status, StravaActivityPage Activities);
