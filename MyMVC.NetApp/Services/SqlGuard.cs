using System.Text.RegularExpressions;

namespace MyMVC.NetApp.Services;

// Validates SQL that Claude generates before it ever reaches SQLite. Defense in depth
// alongside the read-only connection SqlQueryTool opens: even if a forbidden keyword slips
// past here, the file itself can't be mutated.
public static partial class SqlGuard
{
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "REPLACE",
        "ATTACH", "DETACH", "PRAGMA", "VACUUM", "REINDEX", "TRIGGER",
        "BEGIN", "COMMIT", "ROLLBACK", "EXEC", "EXECUTE",
    ];

    public static bool TryValidate(string? sql, out string sanitized, out string? rejectionReason)
    {
        sanitized = string.Empty;

        var trimmed = sql?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            rejectionReason = "SQL statement was empty.";
            return false;
        }

        if (trimmed.EndsWith(';'))
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        if (trimmed.Contains(';'))
        {
            rejectionReason = "Only a single statement is allowed — remove the semicolon(s).";
            return false;
        }

        if (trimmed.Contains("--") || trimmed.Contains("/*"))
        {
            rejectionReason = "Comments are not allowed in the query.";
            return false;
        }

        var firstWord = FirstWordRegex().Match(trimmed) is { Success: true } m ? m.Value.ToUpperInvariant() : string.Empty;
        if (firstWord is not ("SELECT" or "WITH"))
        {
            rejectionReason = "Only SELECT statements are allowed.";
            return false;
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (Regex.IsMatch(trimmed, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                rejectionReason = $"Statement contains a disallowed keyword: {keyword}.";
                return false;
            }
        }

        if (Regex.IsMatch(trimmed, @"\bStravaTokens\b", RegexOptions.IgnoreCase))
        {
            rejectionReason = "StravaTokens cannot be queried (it holds OAuth secrets).";
            return false;
        }

        sanitized = trimmed;
        rejectionReason = null;
        return true;
    }

    [GeneratedRegex(@"^[A-Za-z]+")]
    private static partial Regex FirstWordRegex();
}
