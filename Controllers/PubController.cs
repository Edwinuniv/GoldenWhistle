using GoldenWhistle.Data;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class PubController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PubController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Page load — map is empty, JS will call /api/pubs to populate
        public IActionResult Index() => View();

        // API endpoint called by Leaflet/JS with user's coordinates
        [HttpGet]
        [Route("api/pubs")]
        public async Task<IActionResult> GetPubs(double lat = 0, double lng = 0, double radius = 5000)
        {
            // TODO: once PubLocations table exists, query by distance:
            // var pubs = await _db.PubLocations
            //     .Where(p => p.IsApproved)
            //     .ToListAsync();
            // return Ok(pubs.Select(p => new { ... }));

            // Placeholder — returns empty until Dev A adds PubLocations table
            return Ok(new List<object>());
        }

        [HttpPost]
        [Route("api/pubs/{id}/rate")]
        public async Task<IActionResult> RatePub(int id, [FromBody] RatePubRequest request)
        {
            // TODO: save rating to PubRatings table
            return Ok(new { success = true });
        }
    }

    public class RatePubRequest
    {
        public int Rating { get; set; }
    }
}