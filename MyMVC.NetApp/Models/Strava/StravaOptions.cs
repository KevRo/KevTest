namespace MyMVC.NetApp.Models.Strava;

public class StravaOptions
{
    public const string SectionName = "Strava";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scopes { get; set; } = "read,activity:read_all,profile:read_all";
    public int ActivitiesPageLimit { get; set; } = 5;
    public int ActivitiesPerPage { get; set; } = 200;
}
