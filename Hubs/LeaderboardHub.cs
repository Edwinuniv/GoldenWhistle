using GoldenWhistle.Data;
using GoldenWhistle.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GoldenWhistle.Hubs;

// NEW (audit §2): site.js's initDashboard()/initBracket() both call
// initSignalRConnection('/hubs/leaderboard'), but Program.cs only ever
// mapped '/hubs/moodmap' — the leaderboard hub simply didn't exist, so the
// connection failed (silently swallowed by the .catch in site.js) and no
// live leaderboard updates were ever possible.
//
// This hub doesn't run its own polling loop — call
// BroadcastLeaderboardUpdatedAsync from BracketScoringService right after
// picks are scored (see follow-up note in that service) so real point
// changes push a "LeaderboardUpdated" event to connected clients.
public class LeaderboardHub : Hub
{
    public static async Task BroadcastLeaderboardUpdatedAsync(IHubContext<LeaderboardHub> hub)
    {
        await hub.Clients.All.SendAsync("LeaderboardUpdated", new { updatedAt = DateTime.UtcNow });
    }
}
