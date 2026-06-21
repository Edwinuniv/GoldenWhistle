using GoldenWhistle.Data;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class SimulatorController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SimulatorController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Finished || m.Started)
                .OrderBy(m => m.KickoffUtc)
                .ToListAsync();

            var vm = new SimulatorViewModel
            {
                Matches = matches.Select(m => new SimMatchViewModel
                {
                    MatchId = m.Id,
                    HomeTeamName = m.HomeTeam.Name,
                    HomeTeamCode = m.HomeTeam.ShortName,
                    AwayTeamName = m.AwayTeam.Name,
                    AwayTeamCode = m.AwayTeam.ShortName,
                    HomeScore = m.HomeScore ?? 0,
                    AwayScore = m.AwayScore ?? 0
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [Route("api/simulator/run")]
        public async Task<IActionResult> Run([FromBody] SimulatorRunRequest request)
        {
            // Calculate bracket from adjusted scores
            var winners = request.Matches.Select(m => new
            {
                MatchId = m.MatchId,
                Winner = m.HomeScore > m.AwayScore ? "home" :
                           m.AwayScore > m.HomeScore ? "away" : "draw",
                IsUpset = false // TODO: compare vs original result
            }).ToList();

            // TODO: generate AI narrative via AI service
            var narrative = "In this alternate timeline, the results rewrote history...";

            return Ok(new
            {
                winners,
                narrative,
                winProbabilities = new[]
                {
                    new { team = "TBD", probability = 50 },
                    new { team = "TBD", probability = 50 }
                }
            });
        }
    }

    public class SimulatorRunRequest
    {
        public List<SimMatchScore> Matches { get; set; } = new();
    }

    public class SimMatchScore
    {
        public int MatchId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}