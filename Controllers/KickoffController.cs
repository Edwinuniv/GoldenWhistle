using GoldenWhistle.Data;
using GoldenWhistle.ViewModels.Kickoff;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class KickoffController : Controller
    {
        private readonly ApplicationDbContext _db;

        public KickoffController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.League)
                .Where(m => !m.Cancelled && !m.Finished)
                .OrderBy(m => m.Started ? 0 : 1)
                .ThenBy(m => m.KickoffUtc)
                .Take(10)
                .ToListAsync();

            if (matches.Count == 0)
            {
                return View(new KickoffViewModel { Matches = new List<KickoffMatchViewModel>() });
            }

            var vm = new KickoffViewModel
            {
                Matches = matches.Select(m => new KickoffMatchViewModel
                {
                    MatchId = m.Id,
                    HomeTeamName = m.HomeTeam.Name,
                    HomeTeamCode = m.HomeTeam.ShortName,
                    AwayTeamName = m.AwayTeam.Name,
                    AwayTeamCode = m.AwayTeam.ShortName,
                    KickoffUtc = m.KickoffUtc,
                    StadiumInfo = m.League != null ? $"{m.League.Name} · {m.KickoffUtc:HH:mm} UTC" : $"Match · {m.KickoffUtc:HH:mm} UTC",
                    HomeInjuries = new List<InjuryItemViewModel>(),
                    AwayInjuries = new List<InjuryItemViewModel>(),
                    HomeTactic = null,
                    AwayTactic = null,
                    Facts = new List<FactViewModel>(),
                    H2H = null
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        [Route("api/kickoff/matches")]
        public async Task<IActionResult> GetMatches()
        {
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.League)
                .Where(m => !m.Cancelled && !m.Finished)
                .OrderBy(m => m.KickoffUtc)
                .Take(10)
                .ToListAsync();

            if (matches.Count == 0)
            {
                return Ok(new List<object>());
            }

            var result = matches.Select(m => new
            {
                id = m.Id,
                homeName = m.HomeTeam.Name,
                homeCode = m.HomeTeam.ShortName,
                awayName = m.AwayTeam.Name,
                awayCode = m.AwayTeam.ShortName,
                homeFlag = m.HomeTeam.Country ?? "🏠",
                awayFlag = m.AwayTeam.Country ?? "✈️",
                kickoff = m.KickoffUtc,
                info = m.League != null ? $"{m.League.Name} · {m.KickoffUtc:HH:mm}" : $"Match · {m.KickoffUtc:HH:mm}",
                homeRecord = "W0 D0 L0",
                awayRecord = "W0 D0 L0"
            });

            return Ok(result);
        }
    }
}