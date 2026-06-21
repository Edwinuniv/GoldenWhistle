using GoldenWhistle.Data;
using GoldenWhistle.ViewModels;
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
            // Get upcoming matches with their previews
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => !m.Started && !m.Cancelled)
                .OrderBy(m => m.KickoffUtc)
                .Take(5)
                .ToListAsync();

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
                    StadiumInfo = m.StatusLong,
                    // TODO: populate from MatchPreviews table once Dev A creates it
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
    }
}