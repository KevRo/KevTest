using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Models.Claude;

namespace MyMVC.NetApp.Services;

// Runs Claude-generated SQL against strava.db over a connection opened read-only, as a second
// layer of defense under SqlGuard. Never throws for bad/rejected SQL — callers feed the
// resulting error back to Claude as a retryable tool result.
public class SqlQueryTool : ISqlQueryTool
{
    private readonly string _connectionString;
    private readonly ClaudeOptions _options;
    private readonly ILogger<SqlQueryTool> _logger;

    public SqlQueryTool(IConfiguration configuration, IOptions<ClaudeOptions> options, ILogger<SqlQueryTool> logger)
    {
        var baseConnectionString = configuration.GetConnectionString("Strava") ?? "Data Source=strava.db";
        var builder = new SqliteConnectionStringBuilder(baseConnectionString)
        {
            Mode = SqliteOpenMode.ReadOnly,
        };
        _connectionString = builder.ToString();
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SqlQueryResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        if (!SqlGuard.TryValidate(sql, out var sanitized, out var rejectionReason))
        {
            return new SqlQueryResult(false, rejectionReason, [], [], false);
        }

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sanitized;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            var rows = new List<IReadOnlyList<string?>>();
            var truncated = false;

            while (await reader.ReadAsync(cancellationToken))
            {
                if (rows.Count >= _options.MaxRowsReturned)
                {
                    truncated = true;
                    break;
                }

                var row = new string?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i).ToString();
                }

                rows.Add(row);
            }

            return new SqlQueryResult(true, null, columns, rows, truncated);
        }
        catch (SqliteException ex)
        {
            _logger.LogInformation(ex, "Claude-generated SQL failed to execute: {Sql}", sanitized);
            return new SqlQueryResult(false, ex.Message, [], [], false);
        }
    }
}
