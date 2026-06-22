using GoldenWhistle.Data;
using GoldenWhistle.Hubs;
using GoldenWhistle.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SyncController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFootballApiService _footballApiService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IFootballApiService footballApiService, ILogger<SyncController> logger)
    {
        _footballApiService = footballApiService;
        _logger = logger;
    }

    [HttpGet("live")]
    public async Task<IActionResult> SyncLive()
    {
        try
        {
            _logger.LogInformation("Sync triggered manually.");
            var count = await _footballApiService.SyncLiveMatchesAsync();
            return Ok(new { message = $"Synced {count} live matches successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed.");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    [HttpGet("testvote")]
    public async Task<IActionResult> TestVote()
    {
        // Get the first match in the DB
        var match = await _db.Matches.FirstOrDefaultAsync();
        if (match is null) return NotFound("No matches found.");

        // Force a vote via the hub
        var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<MoodMapHub>>();
        await hubContext.Clients.All.SendAsync("ReceiveTallies", new
        {
            apiMatchId = match.ApiMatchId,
            ecstasy = 5,
            agony = 2,
            anxiety = 1,
            total = 8
        });

        return Ok("Test tally broadcast sent.");
    }
}