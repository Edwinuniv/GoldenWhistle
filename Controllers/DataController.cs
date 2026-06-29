using GoldenWhistle.Data;
using GoldenWhistle.ViewModels;
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
    }

    public class MatchStatsViewModel
    {
        public string HomeTeam { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool IsLive { get; set; }
        public int Minute { get; set; }
        public double HomeXg { get; set; }
        public double AwayXg { get; set; }
        public int HomeShots { get; set; }
        public int AwayShots { get; set; }
        public int HomePossession { get; set; }
        public int AwayPossession { get; set; }
        public int HomePasses { get; set; }
        public int AwayPasses { get; set; }
        public int HomeDuelsWon { get; set; }
        public int AwayDuelsWon { get; set; }
        public string AiSummary { get; set; } = string.Empty;
    }
}