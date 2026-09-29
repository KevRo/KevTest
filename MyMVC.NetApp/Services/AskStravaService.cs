using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Models.Claude;

namespace MyMVC.NetApp.Services;

// Drives a manual Claude tool-use loop (not the beta BetaToolRunner - a single fixed tool
// doesn't need it) with one read-only SQL tool over the local Strava database.
public class AskStravaService : IAskStravaService
{
    private const string QueryToolName = "query_strava_database";

    private const string SystemPrompt = """
        You are answering questions about the user's own local Strava data using the
        query_strava_database tool. Always call the tool to get real numbers before
        answering - never guess or estimate a statistic. If a query is rejected, read
        the error message and retry with corrected SQL. Keep your final answer
        conversational and concise; this is a chat UI, not a report.
        """;

    private const string SqlToolDescription = """
        Runs a single read-only SQLite SELECT (or WITH ... SELECT) statement against the
        user's local Strava database and returns the resulting rows as a tab-separated text table.

        Queryable tables:

        StravaActivities - one row per Strava activity.
          Id (integer, PK), AthleteId (integer), Name (text), Type (text), SportType (text),
          Distance (real, meters), MovingTime (integer, seconds), ElapsedTime (integer, seconds),
          TotalElevationGain (real, meters), StartDateUtc (datetime - sort/filter by date on this),
          StartDateLocal (datetime), Timezone (text), AverageSpeed (real, m/s), MaxSpeed (real, m/s),
          AverageHeartrate (real, bpm), MaxHeartrate (real, bpm), Calories (real),
          KudosCount (integer), AchievementCount (integer), Trainer/Commute/Manual/Private (0/1),
          GearId (text), RawJson (text - the full untruncated Strava API JSON for this activity;
          use SQLite's json_extract(RawJson, '$.path.to.field') for fields not listed above),
          FetchedAtUtc (datetime).

        StravaAthletes - one row, the connected athlete's profile.
          Id, Username, Firstname, Lastname, City, State, Country, Sex, Premium, Summit,
          FollowerCount, FriendCount, MeasurementPreference, Ftp, Weight,
          AllTimeDistanceMeters (real - Strava's own lifetime total, not summed from activities),
          ProfileRawJson, StatsRawJson (full untruncated Strava JSON - use json_extract), FetchedAtUtc.

        Rules: standard SQLite syntax only; exactly one SELECT/WITH statement, no semicolons,
        no comments; results are capped server-side, so add your own ORDER BY / LIMIT /
        aggregation rather than relying on that cap. Never query any other table
        (in particular, StravaTokens holds OAuth secrets and is off-limits).
        """;

    private readonly AnthropicClient _client;
    private readonly ISqlQueryTool _sqlTool;
    private readonly ClaudeOptions _options;
    private readonly ILogger<AskStravaService> _logger;

    public AskStravaService(
        AnthropicClient client,
        ISqlQueryTool sqlTool,
        IOptions<ClaudeOptions> options,
        ILogger<AskStravaService> logger)
    {
        _client = client;
        _sqlTool = sqlTool;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> AskAsync(string question, IReadOnlyList<ChatTurn> priorTurns, CancellationToken cancellationToken = default)
    {
        var messages = BuildHistoryMessages(priorTurns, question);
        var tool = BuildQueryTool();

        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            var parameters = new MessageCreateParams
            {
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                System = SystemPrompt,
                Thinking = new ThinkingConfigAdaptive(),
                Tools = [tool],
                Messages = messages,
            };

            var response = await _client.Messages.Create(parameters, cancellationToken);

            if (response.StopReason == "refusal")
            {
                _logger.LogWarning("Claude declined to answer. Category: {Category}", response.StopDetails?.Category);
                return "Claude declined to answer that question. Try rephrasing it.";
            }

            if (response.StopReason != "tool_use")
            {
                return ExtractText(response);
            }

            var assistantContent = new List<ContentBlockParam>();
            var toolResults = new List<ContentBlockParam>();
            var sawToolUse = false;

            foreach (var block in response.Content)
            {
                if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                {
                    sawToolUse = true;
                    assistantContent.Add(new ToolUseBlockParam { ID = toolUse.ID, Name = toolUse.Name, Input = toolUse.Input });
                    var (resultText, isError) = await RunToolAsync(toolUse, cancellationToken);
                    toolResults.Add(new ToolResultBlockParam { ToolUseID = toolUse.ID, Content = resultText, IsError = isError });
                }
                else if (block.TryPickText(out TextBlock? text))
                {
                    assistantContent.Add(new TextBlockParam { Text = text.Text });
                }
                else if (block.TryPickThinking(out ThinkingBlock? thinking))
                {
                    assistantContent.Add(new ThinkingBlockParam { Thinking = thinking.Thinking, Signature = thinking.Signature });
                }
            }

            if (!sawToolUse)
            {
                return ExtractText(response);
            }

            messages =
            [
                .. messages,
                new MessageParam { Role = Role.Assistant, Content = assistantContent },
                new MessageParam { Role = Role.User, Content = toolResults },
            ];
        }

        throw new AskStravaToolLoopExceededException(
            $"Did not reach a final answer within {_options.MaxToolIterations} tool-use iterations.");
    }

    private static List<MessageParam> BuildHistoryMessages(IReadOnlyList<ChatTurn> priorTurns, string question)
    {
        var messages = new List<MessageParam>();
        foreach (var turn in priorTurns)
        {
            messages.Add(new MessageParam { Role = Role.User, Content = turn.Question });
            messages.Add(new MessageParam { Role = Role.Assistant, Content = turn.Answer });
        }

        messages.Add(new MessageParam { Role = Role.User, Content = question });
        return messages;
    }

    private static Tool BuildQueryTool() => new()
    {
        Name = QueryToolName,
        Description = SqlToolDescription,
        InputSchema = new InputSchema
        {
            Type = JsonSerializer.SerializeToElement("object"),
            Properties = new Dictionary<string, JsonElement>
            {
                ["sql"] = JsonSerializer.SerializeToElement(new
                {
                    type = "string",
                    description = "A single standard SQLite SELECT (or WITH ... SELECT) statement. No semicolons, no comments, no data-modifying statements.",
                }),
            },
            Required = ["sql"],
        },
    };

    private async Task<(string ResultText, bool IsError)> RunToolAsync(ToolUseBlock toolUse, CancellationToken cancellationToken)
    {
        if (!string.Equals(toolUse.Name, QueryToolName, StringComparison.Ordinal))
        {
            return ($"Unknown tool '{toolUse.Name}'.", true);
        }

        if (toolUse.Input is null
            || !toolUse.Input.TryGetValue("sql", out var sqlElement)
            || sqlElement.ValueKind != JsonValueKind.String)
        {
            return ("Missing or invalid 'sql' string input.", true);
        }

        var result = await _sqlTool.ExecuteAsync(sqlElement.GetString() ?? string.Empty, cancellationToken);

        if (!result.Success)
        {
            return ($"Query rejected: {result.ErrorMessage}", true);
        }

        return (SerializeResult(result), false);
    }

    private string SerializeResult(SqlQueryResult result)
    {
        if (result.Rows.Count == 0)
        {
            return "(no rows)";
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Join('\t', result.Columns));
        foreach (var row in result.Rows)
        {
            sb.AppendLine(string.Join('\t', row.Select(v => v ?? "")));
        }

        var text = sb.ToString();
        if (text.Length > _options.MaxResultCharacters)
        {
            text = text[.._options.MaxResultCharacters] + "\n[truncated - output too long, narrow your query]";
        }
        else if (result.Truncated)
        {
            text += $"\n[truncated - only the first {result.Rows.Count} rows are shown, narrow your query]";
        }

        return text;
    }

    private static string ExtractText(Message response)
    {
        var text = string.Concat(response.Content
            .Select(b => b.TryPickText(out TextBlock? t) ? t.Text : null)
            .Where(t => !string.IsNullOrEmpty(t)));

        return string.IsNullOrWhiteSpace(text)
            ? "I couldn't come up with an answer to that - try rephrasing the question."
            : text;
    }
}
