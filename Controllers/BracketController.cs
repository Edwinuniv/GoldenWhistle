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
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .OrderBy(m => m.KickoffUtc)
                .ToListAsync();

            var liveEvents = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Started && !m.Finished)
                .ToListAsync();

            var vm = new BracketViewModel
            {
                TotalCorrect = 0, // TODO: from BracketPicks table
                TotalPending = 0, // TODO: from BracketPicks table
                LeagueName = "My League", // TODO: from PrivateLeagues table

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

                LeagueStandings = await _db.Users
                    .OrderByDescending(u => u.TotalPoints)
                    .Take(10)
                    .Select((u, i) => new LeagueStandingViewModel
                    {
                        Rank = i + 1,
                        UserName = u.DisplayName ?? u.UserName ?? "Fan",
                        CorrectPicks = 0, // TODO: from BracketPicks
                        Points = u.TotalPoints
                    }).ToListAsync(),

                LiveEvents = new List<LiveEventViewModel>() // TODO: from live match events
            };

            return View(vm);
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