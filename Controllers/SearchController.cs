using GoldenWhistle.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    // NEW (audit §2): the topbar's global search box called
    // GET /api/search?q=... on every keystroke, but no SearchController
    // (or equivalent route) existed anywhere in the codebase — the search
    // dropdown could never return results. This implements a minimal,
    // real-data search across teams and matches (extend with players/
    // listings/pubs as those models grow appropriate name fields).
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public SearchController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(Array.Empty<object>());

            var results = new List<object>();

            var teams = await _db.Teams
                .Where(t => t.Name.Contains(q) || t.ShortName.Contains(q))
                .Take(5)
                .ToListAsync();

            results.AddRange(teams.Select(t => new
            {
                label = t.Name,
                type = "Team",
                url = $"/Kickoff?team={t.Id}"
            }));

            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.HomeTeam.Name.Contains(q) || m.AwayTeam.Name.Contains(q))
                .OrderByDescending(m => m.KickoffUtc)
                .Take(5)
                .ToListAsync();

            results.AddRange(matches.Select(m => new
            {
                label = $"{m.HomeTeam.Name} vs {m.AwayTeam.Name}",
                type = "Match",
                url = $"/Bracket#match-{m.Id}"
            }));

            return Ok(results.Take(10));
        }
    }
}
