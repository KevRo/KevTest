using MyMVC.NetApp.Models.Claude;

namespace MyMVC.NetApp.Services;

public interface IAskStravaService
{
    Task<string> AskAsync(string question, IReadOnlyList<ChatTurn> priorTurns, CancellationToken cancellationToken = default);
}
