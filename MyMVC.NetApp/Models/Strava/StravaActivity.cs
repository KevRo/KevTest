namespace MyMVC.NetApp.Models.Strava;

public class StravaActivity
{
    public long Id { get; set; }
    public long AthleteId { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? SportType { get; set; }
    public double Distance { get; set; }
    public int MovingTime { get; set; }
    public int ElapsedTime { get; set; }
    public double TotalElevationGain { get; set; }
    public DateTimeOffset StartDateUtc { get; set; }
    public DateTimeOffset StartDateLocal { get; set; }
    public string? Timezone { get; set; }
    public double? AverageSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? AverageHeartrate { get; set; }
    public double? MaxHeartrate { get; set; }
    public double? Calories { get; set; }
    public int KudosCount { get; set; }
    public int AchievementCount { get; set; }
    public bool Trainer { get; set; }
    public bool Commute { get; set; }
    public bool Manual { get; set; }
    public bool Private { get; set; }
    public string? GearId { get; set; }

    // Full activity JSON as returned by Strava, kept verbatim alongside the columns above.
    public string RawJson { get; set; } = string.Empty;

    public DateTime FetchedAtUtc { get; set; }
}
