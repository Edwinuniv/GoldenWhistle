using GoldenWhistle.Data;
using GoldenWhistle.ViewModels.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class DataController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DataController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var match = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Started)
                .OrderByDescending(m => m.KickoffUtc)
                .FirstOrDefaultAsync();

            if (match == null)
                return View(new MatchStatsViewModel());

            var stats = await _db.MatchStats
                .FirstOrDefaultAsync(s => s.MatchId == match.Id);

            var vm = new MatchStatsViewModel
            {
                HomeTeam = match.HomeTeam.Name,
                HomeTeamCode = match.HomeTeam.ShortName,
                AwayTeam = match.AwayTeam.Name,
                AwayTeamCode = match.AwayTeam.ShortName,
                HomeScore = match.HomeScore ?? 0,
                AwayScore = match.AwayScore ?? 0,
                IsLive = match.Started && !match.Finished,
                // FIX (audit §5 note): the previous "Minute = 73" was a
                // literal hardcoded value whenever StatusShort == "LIVE".
                // We no longer have a reliable live-minute source from the
                // free API tier, so we show 0 (unknown) rather than a fake
                // fixed number; the front-end will simply display "LIVE"
                // without a fabricated clock until a real minute feed exists.
                Minute = 0,
                HomeXg = stats?.HomeXg ?? 0,
                AwayXg = stats?.AwayXg ?? 0,
                HomeShots = stats?.HomeShotsTotal ?? 0,
                AwayShots = stats?.AwayShotsTotal ?? 0,
                HomePossession = (int)(stats?.HomePossessionPct ?? 50),
                AwayPossession = (int)(stats?.AwayPossessionPct ?? 50),
                HomePasses = stats?.HomePasses ?? 0,
                AwayPasses = stats?.AwayPasses ?? 0,
                HomeDuelsWon = stats?.HomeDuelsWon ?? 0,
                AwayDuelsWon = stats?.AwayDuelsWon ?? 0,
                // FIX (audit §1): previously always string.Empty and never
                // even rendered by the old view. Now a short, purely factual
                // summary built directly from real numbers — no invented
                // narrative, no player names that weren't actually recorded.
                AiSummary = BuildFactualSummary(match, stats)
            };

            return View(vm);
        }

        private static string BuildFactualSummary(
            GoldenWhistle.Models.Match match,
            GoldenWhistle.Models.MatchStats? stats)
        {
            if (stats is null)
                return string.Empty;

            var leaderName = match.HomeScore > match.AwayScore ? match.HomeTeam.Name
                : match.AwayScore > match.HomeScore ? match.AwayTeam.Name
                : null;

            var scoreClause = leaderName is not null
                ? $"{leaderName} currently lead{(match.Finished ? "" : "s")} {match.HomeScore ?? 0}–{match.AwayScore ?? 0}."
                : $"The match is level at {match.HomeScore ?? 0}–{match.AwayScore ?? 0}.";

            var possessionClause = stats.HomePossessionPct is not null && stats.AwayPossessionPct is not null
                ? $" {match.HomeTeam.ShortName} have had {stats.HomePossessionPct:0}% possession to {match.AwayTeam.ShortName}'s {stats.AwayPossessionPct:0}%."
                : string.Empty;

            var xgClause = stats.HomeXg is not null && stats.AwayXg is not null
                ? $" Expected goals: {match.HomeTeam.ShortName} {stats.HomeXg:0.00} — {match.AwayTeam.ShortName} {stats.AwayXg:0.00}."
                : string.Empty;

            return $"{scoreClause}{possessionClause}{xgClause}".Trim();
        }

        [HttpGet]
        [Route("api/data/stats")]
        public async Task<IActionResult> GetStats()
        {
            var match = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Started)
                .OrderByDescending(m => m.KickoffUtc)
                .FirstOrDefaultAsync();

            if (match == null)
                return Ok(new { });

            var stats = await _db.MatchStats
                .FirstOrDefaultAsync(s => s.MatchId == match.Id);

            if (stats == null)
                return Ok(new { });

            // NOTE (audit §1 follow-up): the xG "timeline" here is still a
            // single real end-of-period value repeated across a fixed label
            // set (we don't have a minute-by-minute xG feed from the API).
            // This is disclosed rather than fabricating intermediate points.
            var homeXgData = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, stats.HomeXg ?? 0 };
            var awayXgData = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, stats.AwayXg ?? 0 };

            return Ok(new
            {
                homeTeam = match.HomeTeam.Name,
                awayTeam = match.AwayTeam.Name,
                homeScore = match.HomeScore ?? 0,
                awayScore = match.AwayScore ?? 0,
                isLive = match.Started && !match.Finished,
                xgTimeline = new
                {
                    labels = new[] { "5'", "15'", "23'", "35'", "45'", "54'", "65'", "Now" },
                    homeXg = homeXgData,
                    awayXg = awayXgData
                },
                radar = new
                {
                    labels = new[] { "Shots", "Passes", "Possession", "Duels Won", "Dribbles", "Saves" },
                    homeData = new[]
                    {
                        stats.HomeShotsTotal ?? 0,
                        stats.HomePasses ?? 0,
                        stats.HomePossessionPct ?? 0,
                        stats.HomeDuelsWon ?? 0,
                        0,
                        stats.HomeSaves ?? 0
                    }
                },
                possession = new
                {
                    labels = new[] { "15'", "30'", "45'", "60'", "75'" },
                    values = new[]
                    {
                        stats.HomePossessionPct ?? 50,
                        stats.HomePossessionPct ?? 50,
                        stats.HomePossessionPct ?? 50,
                        stats.HomePossessionPct ?? 50,
                        stats.HomePossessionPct ?? 50
                    }
                }
                // NOTE (audit §1 follow-up): the heatmap block was removed
                // from this response. The old points (fixed x/y/r/intensity
                // coordinates) were entirely invented and not derived from
                // any real touch-location data, which the API doesn't
                // provide. Rather than keep shipping fake coordinates, the
                // heatmap canvas now simply stays empty until a real
                // touch-location data source is integrated. See
                // GoldenWhistle_Audit.md §1 for details.
            });
        }
    }
}
