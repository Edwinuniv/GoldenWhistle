using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class BracketController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public BracketController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;

            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .OrderBy(m => m.KickoffUtc)
                .ToListAsync();

            var userPicks = await _db.BracketPicks
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.MatchId, p => p);

            var leagueName = "My League";
            var userLeague = await _db.LeagueMembers
                .Where(lm => lm.UserId == userId)
                .Include(lm => lm.League)
                .FirstOrDefaultAsync();

            if (userLeague != null)
                leagueName = userLeague.League.Name;

            var scoredPicks = userPicks.Values.Where(p => p.IsScored).ToList();
            var totalCorrect = scoredPicks.Count(p => p.PointsAwarded > 0);
            var totalPending = matches.Count(m => !m.Finished && !m.Cancelled);

            var vm = new BracketViewModel
            {
                TotalCorrect = totalCorrect,
                TotalPending = totalPending,
                LeagueName = leagueName,
                Picks = matches.Select(m => new BracketMatchViewModel
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
                LeagueStandings = await GetLeagueStandingsAsync(userId),
                LiveEvents = new List<LiveEventViewModel>()
            };

            return View(vm);
        }

        // ✅ CORRIGÉ
        private async Task<List<LeagueStandingViewModel>> GetLeagueStandingsAsync(string userId)
        {
            var userLeague = await _db.LeagueMembers
                .Where(lm => lm.UserId == userId)
                .Select(lm => lm.PrivateLeagueId)
                .FirstOrDefaultAsync();

            if (userLeague == 0)
            {
                // Récupérer les données d'abord
                var users = await _db.Users
                    .OrderByDescending(u => u.TotalPoints)
                    .Take(10)
                    .ToListAsync();

                // Calculer le rang en mémoire
                return users
                    .Select((u, i) => new LeagueStandingViewModel
                    {
                        Rank = i + 1,
                        UserName = u.DisplayName ?? u.UserName ?? "Fan",
                        Points = u.TotalPoints
                    }).ToList();
            }

            var members = await _db.LeagueMembers
                .Where(lm => lm.PrivateLeagueId == userLeague)
                .Include(lm => lm.User)
                .ToListAsync();

            // Trier et calculer le rang en mémoire
            return members
                .OrderByDescending(m => m.User.TotalPoints)
                .Select((m, i) => new LeagueStandingViewModel
                {
                    Rank = i + 1,
                    UserName = m.User.DisplayName ?? m.User.UserName ?? "Fan",
                    Points = m.User.TotalPoints
                }).ToList();
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