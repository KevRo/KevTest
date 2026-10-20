using MyMVC.NetApp.Models.Claude;

namespace MyMVC.NetApp.Services;

// E2E-only stand-in for AskStravaService (see Program.cs's IsEnvironment("E2E") gate).
// Still drives the real ISqlQueryTool/SqlGuard against the seeded e2e Strava database, so
// tests exercise the actual tool-call plumbing end-to-end - only the Anthropic network call
// itself is replaced, since real calls would be non-deterministic, costly, and (currently)
// blocked by the configured account's credit balance anyway.
public class FakeAskStravaService : IAskStravaService
{
    private readonly ISqlQueryTool _sqlTool;

    public FakeAskStravaService(ISqlQueryTool sqlTool)
    {
        _sqlTool = sqlTool;
    }

    public async Task<string> AskAsync(string question, IReadOnlyList<ChatTurn> priorTurns, CancellationToken cancellationToken = default)
    {
        var result = await _sqlTool.ExecuteAsync("SELECT COUNT(*) FROM StravaActivities", cancellationToken);
        if (!result.Success)
        {
            return $"(fake) query failed: {result.ErrorMessage}";
        }

        var count = result.Rows.Count > 0 ? result.Rows[0][0] : "0";
        return $"(fake) You have {count} activities synced. You asked: \"{question}\"";
    }
}
