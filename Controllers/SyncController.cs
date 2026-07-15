using GoldenWhistle.Data;
using GoldenWhistle.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GoldenWhistle.Controllers;

// FIX (audit §5, critique): this controller previously had NO authorization
// at all on /api/sync/live and /api/sync/stats. Anyone on the internet could
// call these repeatedly, each call hitting the paid RapidAPI football
// endpoint — a direct cost/DoS exposure. We now require a shared secret
// header, checked against configuration (appsettings / environment
// variable), so only your own scheduler/background job (or an admin) can
// trigger a sync. If you already trigger syncs exclusively from
// SyncBackgroundService in-process, consider removing the public HTTP routes
// entirely and keeping this only as an internal fallback.
[Route("api/[controller]")]
[ApiController]
public class SyncController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFootballApiService _footballApiService;
    private readonly IBracketScoringService _scoringService;
    private readonly IMatchStatsService _matchStatsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ApplicationDbContext db,
        IFootballApiService footballApiService,
        IBracketScoringService scoringService,
        IMatchStatsService matchStatsService,
        IConfiguration configuration,
        ILogger<SyncController> logger)
    {
        _db = db;
        _footballApiService = footballApiService;
        _scoringService = scoringService;
        _matchStatsService = matchStatsService;
        _configuration = configuration;
        _logger = logger;
    }

    private bool IsAuthorizedSyncCaller()
    {
        var expectedKey = _configuration["SyncApi:Key"];
        if (string.IsNullOrEmpty(expectedKey))
        {
            // Fail closed: if no key is configured, refuse rather than
            // silently allow public access.
            _logger.LogWarning("SyncApi:Key is not configured — refusing sync request.");
            return false;
        }

        return Request.Headers.TryGetValue("X-Sync-Key", out var provided)
            && provided == expectedKey;
    }

    [HttpGet("live")]
    public async Task<IActionResult> SyncLive()
    {
        if (!IsAuthorizedSyncCaller()) return Unauthorized();

        try
        {
            _logger.LogInformation("Sync triggered manually.");

            var matchCount = await _footballApiService.SyncLiveMatchesAsync();
            var scoredCount = await _scoringService.ScoreFinishedMatchesAsync();
            var statsCount = await _matchStatsService.SyncMatchStatsAsync();

            return Ok(new
            {
                message = $"Synced {matchCount} live matches, " +
                          $"scored {scoredCount} picks, " +
                          $"fetched stats for {statsCount} matches."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> SyncStats()
    {
        if (!IsAuthorizedSyncCaller()) return Unauthorized();

        try
        {
            _logger.LogInformation("Stats sync triggered manually.");
            var count = await _matchStatsService.SyncMatchStatsAsync();
            return Ok(new { message = $"Synced stats for {count} matches." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stats sync failed.");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // REMOVED (audit §1 & §5, critique): /api/sync/testvote had no auth and
    // broadcast entirely hardcoded fake tallies (ecstasy=5, agony=2,
    // anxiety=1, total=8) to every connected SignalR client, without even
    // persisting anything to the database. This was fake data pushed live
    // to real users and has been deleted. For local manual testing, trigger
    // MoodMapHub.CastVote (or the /api/mood/vote endpoint, authenticated)
    // from a real signed-in test account instead.
}
