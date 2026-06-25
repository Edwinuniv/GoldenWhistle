using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.UtcNow.Date;

            var fixtures = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.KickoffUtc.Date == today)
                .OrderBy(m => m.KickoffUtc)
                .Take(4)
                .ToListAsync();

            var topLeaders = await _db.Users
                .OrderByDescending(u => u.TotalPoints)
                .Take(3)
                .ToListAsync();

            var liveMatch = fixtures.FirstOrDefault(m => m.Started && !m.Finished);
            int ecstasyPct = 0, anxietyPct = 0, agonyPct = 0, totalVotes = 0;

            if (liveMatch != null)
            {
                var votes = await _db.MoodVotes.Where(v => v.MatchId == liveMatch.Id).ToListAsync();
                totalVotes = votes.Count;
                ecstasyPct = totalVotes > 0 ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Ecstasy) * 100.0 / totalVotes) : 0;
                anxietyPct = totalVotes > 0 ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Anxiety) * 100.0 / totalVotes) : 0;
                agonyPct = totalVotes > 0 ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Agony) * 100.0 / totalVotes) : 0;
            }

            var vm = new DashboardViewModel
            {
                UserDisplayName = user?.DisplayName ?? user?.UserName ?? "Fan",
                UserTotalPoints = user?.TotalPoints ?? 0,
                UserPointsDeltaToday = 0,
                UserPredictionsMade = 0,
                UserAccuracyPct = 0,
                UserBracketRank = 0,
                TotalPlayers = await _db.Users.CountAsync(),

                Fixtures = fixtures.Select(m => new FixtureCardViewModel
                {
                    MatchId = m.Id,
                    HomeTeamCode = m.HomeTeam.ShortName,
                    AwayTeamCode = m.AwayTeam.ShortName,
                    HomeTeamName = m.HomeTeam.Name,
                    AwayTeamName = m.AwayTeam.Name,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    IsLive = m.Started && !m.Finished,
                    StatusBadge = m.Finished ? "FT" : m.Started ? "LIVE" : "UPCOMING",
                    KickoffTime = m.KickoffUtc.ToLocalTime().ToString("HH:mm"),
                    MatchDate = m.KickoffUtc.ToLocalTime().ToString("MMM dd")
                }).ToList(),

                TopLeaders = topLeaders.Select((u, i) => new LeaderRowViewModel
                {
                    Rank = i + 1,
                    UserName = u.DisplayName ?? u.UserName ?? "Fan",
                    Points = u.TotalPoints,
                    PointsDelta = 0
                }).ToList(),

                MoodEcstasyPct = ecstasyPct,
                MoodAnxietyPct = anxietyPct,
                MoodAgonyPct = agonyPct,
                MoodTotalVotes = totalVotes,
                XgByMatch = new List<XgDataPoint>(),
                BracketMatches = new List<BracketMatchViewModel>()
            };

            return View(vm);
        }
    }
}