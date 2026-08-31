using MyMVC.NetApp.Models.Strava;

namespace MyMVC.NetApp.Services;

public interface IStravaSyncService
{
    Task<StravaSyncResult> SyncAsync(string code, CancellationToken cancellationToken = default);

    Task<StravaSyncStatus?> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<StravaActivityPage> GetActivitiesAsync(long athleteId, int page, int pageSize, CancellationToken cancellationToken = default);
}
