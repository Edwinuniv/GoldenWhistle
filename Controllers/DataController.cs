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
            // Get the most recent live or finished match
            var match = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Started)
                .OrderByDescending(m => m.KickoffUtc)
                .FirstOrDefaultAsync();

            if (match == null) return View(new MatchStatsViewModel());

            // TODO: join with MatchStats table once Dev A creates it
            var vm = new MatchStatsViewModel
            {
                HomeTeam = match.HomeTeam.Name,
                HomeTeamCode = match.HomeTeam.ShortName,
                AwayTeam = match.AwayTeam.Name,
                AwayTeamCode = match.AwayTeam.ShortName,
                HomeScore = match.HomeScore ?? 0,
                AwayScore = match.AwayScore ?? 0,
                IsLive = match.Started && !match.Finished,
                Minute = 0, // TODO: from live API feed
                AiSummary = string.Empty // TODO: from AI generation service
            };

            return View(vm);
        }
    }

    // Temporary ViewModel until Dev A adds MatchStats to ViewModels.cs
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