using MyMVC.NetApp.Models.Strava;

namespace MyMVC.NetApp.Services;

public interface IStravaSyncService
{
    Task<StravaSyncResult> SyncAsync(string code, CancellationToken cancellationToken = default);

    Task<StravaSyncStatus?> GetStatusAsync(CancellationToken cancellationToken = default);
}
