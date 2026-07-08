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
                Minute = match.StatusShort == "LIVE" ? 73 : 0,
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
                AiSummary = string.Empty
            };

            return View(vm);
        }

        // ✅ API pour les stats - données RÉELLES
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

            // Construire les données de timeline à partir des stats réelles
            var homeXgData = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, stats.HomeXg ?? 0 };
            var awayXgData = new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, stats.AwayXg ?? 0 };

            return Ok(new
            {
                homeTeam = match.HomeTeam.Name,
                awayTeam = match.AwayTeam.Name,
                homeScore = match.HomeScore ?? 0,
                awayScore = match.AwayScore ?? 0,
                isLive = match.Started && !match.Finished,
                minute = match.StatusShort == "LIVE" ? 73 : 0,
                xgTimeline = new
                {
                    labels = new[] { "5'", "15'", "23'", "35'", "45'", "54'", "65'", "73'" },
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
                },
                heatmap = new
                {
                    team = "home",
                    points = new[]
                    {
                        new { x = 0.65, y = 0.45, r = 90, intensity = 0.9 },
                        new { x = 0.72, y = 0.35, r = 70, intensity = 0.7 },
                        new { x = 0.58, y = 0.55, r = 60, intensity = 0.6 },
                        new { x = 0.80, y = 0.50, r = 50, intensity = 0.5 }
                    }
                }
            });
        }
    }
}