using System.Text.Json;
using Anthropic.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Models.Claude;
using MyMVC.NetApp.Services;

namespace MyMVC.NetApp.Controllers;

public class AskStravaController : Controller
{
    private const string ThreadSessionKey = "AskStrava_Thread";
    private const int MaxStoredTurns = 20;

    private readonly IAskStravaService _askStravaService;
    private readonly ClaudeOptions _options;
    private readonly ILogger<AskStravaController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AskStravaController(
        IAskStravaService askStravaService,
        IOptions<ClaudeOptions> options,
        ILogger<AskStravaController> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _askStravaService = askStravaService;
        _options = options.Value;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult Index() => View(new AskStravaViewModel(LoadThread()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(string question, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Ask Strava requested but Claude:ApiKey is not configured.");
            TempData["AskStravaError"] = "ConfigMissing";
            return RedirectToAction(nameof(Index));
        }

        var thread = LoadThread();

        try
        {
            var answer = await _askStravaService.AskAsync(question.Trim(), thread, cancellationToken);
            thread.Add(new ChatTurn(question.Trim(), answer, DateTime.UtcNow));
            SaveThread(thread);
        }
        catch (AnthropicUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Claude API rejected the configured API key.");
            TempData["AskStravaError"] = "AuthFailed";
        }
        catch (AnthropicRateLimitException ex)
        {
            _logger.LogWarning(ex, "Claude API rate limit hit.");
            TempData["AskStravaError"] = "RateLimited";
        }
        catch (AskStravaToolLoopExceededException ex)
        {
            _logger.LogWarning(ex, "Ask Strava tool loop exceeded its iteration cap.");
            TempData["AskStravaError"] = "ToolLoopExceeded";
        }
        catch (AnthropicApiException ex)
        {
            _logger.LogWarning(ex, "Claude API call failed.");
            TempData["AskStravaError"] = "ApiError";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(ThreadSessionKey);
        return RedirectToAction(nameof(Index));
    }

    private List<ChatTurn> LoadThread()
    {
        var json = HttpContext.Session.GetString(ThreadSessionKey);
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<ChatTurn>>(json) ?? [];
    }

    private void SaveThread(List<ChatTurn> thread)
    {
        if (thread.Count > MaxStoredTurns)
        {
            thread.RemoveRange(0, thread.Count - MaxStoredTurns);
        }

        HttpContext.Session.SetString(ThreadSessionKey, JsonSerializer.Serialize(thread));
    }
}
