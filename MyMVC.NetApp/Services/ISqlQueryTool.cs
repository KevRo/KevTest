namespace MyMVC.NetApp.Services;

public record SqlQueryResult(
    bool Success,
    string? ErrorMessage,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string?>> Rows,
    bool Truncated);

public interface ISqlQueryTool
{
    Task<SqlQueryResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default);
}
