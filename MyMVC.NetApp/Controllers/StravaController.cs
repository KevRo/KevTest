using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Models.Strava;
using MyMVC.NetApp.Services;

namespace MyMVC.NetApp.Controllers;

public class StravaController : Controller
{
    private const string StateCookieName = "strava_oauth_state";

    private readonly IStravaApiClient _stravaApiClient;
    private readonly IStravaSyncService _syncService;
    private readonly StravaOptions _options;
    private readonly ILogger<StravaController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public StravaController(
        IStravaApiClient stravaApiClient,
        IStravaSyncService syncService,
        IOptions<StravaOptions> options,
        ILogger<StravaController> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _stravaApiClient = stravaApiClient;
        _syncService = syncService;
        _options = options.Value;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var status = await _syncService.GetStatusAsync(cancellationToken);
        return View(status);
    }

    [HttpGet]
    public IActionResult Connect()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ClientSecret)
            || string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            _logger.LogWarning("Strava Connect requested but Strava:ClientId/ClientSecret/RedirectUri are not configured.");
            TempData["StravaError"] = "ConfigMissing";
            return RedirectToAction(nameof(Index));
        }

        var state = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(StateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
        });

        return Redirect(_stravaApiClient.BuildAuthorizeUrl(state));
    }

    [HttpGet]
    public async Task<IActionResult> Callback(string? code, string? state, string? error, CancellationToken cancellationToken)
    {
        var expectedState = Request.Cookies[StateCookieName];
        Response.Cookies.Delete(StateCookieName);

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Strava OAuth authorization was denied or errored: {Error}", error);
            TempData["StravaError"] = "Denied";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrEmpty(code)
            || string.IsNullOrEmpty(state)
            || string.IsNullOrEmpty(expectedState)
            || !string.Equals(state, expectedState, StringComparison.Ordinal))
        {
            _logger.LogWarning("Strava OAuth callback failed state validation.");
            TempData["StravaError"] = "InvalidState";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await _syncService.SyncAsync(code, cancellationToken);
            TempData["StravaSyncSummary"] = $"{result.AthleteName}|{result.ActivitiesPulled}";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Strava API call failed during sync.");
            TempData["StravaError"] = "ApiError";
        }

        return RedirectToAction(nameof(Index));
    }
}
