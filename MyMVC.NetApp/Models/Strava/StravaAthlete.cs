namespace MyMVC.NetApp.Models.Strava;

public class StravaAthlete
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Sex { get; set; }
    public bool Premium { get; set; }
    public bool Summit { get; set; }
    public DateTimeOffset? StravaCreatedAt { get; set; }
    public DateTimeOffset? StravaUpdatedAt { get; set; }
    public string? ProfileMediumUrl { get; set; }
    public string? ProfileUrl { get; set; }
    public int? FollowerCount { get; set; }
    public int? FriendCount { get; set; }
    public string? MeasurementPreference { get; set; }
    public int? Ftp { get; set; }
    public double? Weight { get; set; }

    // All-time ride + run + swim distance, from Strava's own lifetime stats (not summed
    // from locally-stored activities, so it's accurate even before/without a full activity pull).
    public double AllTimeDistanceMeters { get; set; }

    // Full API responses, kept verbatim so no field Strava returns is ever lost
    // even if it isn't mapped to a column above.
    public string ProfileRawJson { get; set; } = string.Empty;
    public string? StatsRawJson { get; set; }

    public DateTime FetchedAtUtc { get; set; }
}
