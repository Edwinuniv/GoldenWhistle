using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels.Dashboard;   // ✅ Pour DashboardViewModel, FixtureCardViewModel, LeaderRowViewModel, XgDataPoint
using GoldenWhistle.ViewModels.Bracket;     // ✅ Pour BracketMatchViewModel
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
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var today = DateTime.UtcNow.Date;

            // ─── Fixtures du jour ──────────────────────────────────────
            var fixtures = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.KickoffUtc.Date == today)
                .OrderBy(m => m.KickoffUtc)
                .Take(4)
                .ToListAsync();

            // ─── Top 3 leaders ────────────────────────────────────────
            var topLeaders = await _db.Users
                .OrderByDescending(u => u.TotalPoints)
                .Take(3)
                .ToListAsync();

            // ─── Bracket matches ──────────────────────────────────────
            var bracketMatches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Started || m.Finished)
                .OrderBy(m => m.KickoffUtc)
                .ToListAsync();

            // ─── xG data ──────────────────────────────────────────────
            var matchStats = await _db.MatchStats
                .Include(s => s.Match)
                    .ThenInclude(m => m.HomeTeam)
                .Include(s => s.Match)
                    .ThenInclude(m => m.AwayTeam)
                .Where(s => s.Match.Started || s.Match.Finished)
                .OrderByDescending(s => s.FetchedAt)
                .Take(10)
                .ToListAsync();

            // ─── Mood votes ────────────────────────────────────────────
            var liveMatch = fixtures.FirstOrDefault(m => m.Started && !m.Finished);
            int ecstasyPct = 0, anxietyPct = 0, agonyPct = 0, totalVotes = 0;

            if (liveMatch != null)
            {
                var votes = await _db.MoodVotes
                    .Where(v => v.MatchId == liveMatch.Id)
                    .ToListAsync();
                totalVotes = votes.Count;
                ecstasyPct = totalVotes > 0
                    ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Ecstasy) * 100.0 / totalVotes)
                    : 0;
                anxietyPct = totalVotes > 0
                    ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Anxiety) * 100.0 / totalVotes)
                    : 0;
                agonyPct = totalVotes > 0
                    ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Agony) * 100.0 / totalVotes)
                    : 0;
            }

            // ─── User picks stats ──────────────────────────────────────
            var userPicks = await _db.BracketPicks
                .Where(p => p.UserId == userId && p.IsScored)
                .ToListAsync();

            var totalPicks = userPicks.Count;
            var correctPicks = userPicks.Count(p => p.PointsAwarded > 0);

            var vm = new DashboardViewModel
            {
                UserDisplayName = user?.DisplayName ?? user?.UserName ?? "Fan",
                UserTotalPoints = user?.TotalPoints ?? 0,
                UserPointsDeltaToday = 0,
                UserPredictionsMade = totalPicks,
                UserAccuracyPct = totalPicks > 0
                    ? (int)Math.Round(correctPicks * 100.0 / totalPicks)
                    : 0,
                UserBracketRank = await GetUserRankAsync(userId),
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

                BracketMatches = bracketMatches.Select(m => new BracketMatchViewModel
                {
                    Round = GetRound(m.StatusShort),
                    HomeTeamCode = m.HomeTeam.ShortName,
                    HomeTeamName = m.HomeTeam.Name,
                    AwayTeamCode = m.AwayTeam.ShortName,
                    AwayTeamName = m.AwayTeam.Name,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    KickoffTime = m.KickoffUtc.ToLocalTime().ToString("HH:mm"),
                    IsLive = m.Started && !m.Finished,
                    IsWinner = m.Finished && m.HomeScore > m.AwayScore
                }).ToList(),

                XgByMatch = matchStats.Select(s => new XgDataPoint
                {
                    MatchLabel = $"{s.Match.HomeTeam.ShortName} vs {s.Match.AwayTeam.ShortName}",
                    XgValue = s.HomeXg ?? 0
                }).ToList(),

                MoodEcstasyPct = ecstasyPct,
                MoodAnxietyPct = anxietyPct,
                MoodAgonyPct = agonyPct,
                MoodTotalVotes = totalVotes
            };

            return View(vm);
        }

        private async Task<int> GetUserRankAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return 0;

                var users = await _db.Users
                    .OrderByDescending(u => u.TotalPoints)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (users.Count == 0)
                    return 0;

                var rank = users.IndexOf(userId) + 1;
                return rank > 0 ? rank : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetRound(string statusShort) => statusShort switch
        {
            "QF" => "QF",
            "SF" => "SF",
            "F" => "FINAL",
            _ => "QF"
        };
    }
}