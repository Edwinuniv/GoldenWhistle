using GoldenWhistle.Data;
using GoldenWhistle.Models;
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

        public IActionResult Index() => View();

        // ✅ GET: /api/pubs
        [HttpGet]
        [Route("api/pubs")]
        public async Task<IActionResult> GetPubs(double lat = 0, double lng = 0, double radius = 5000)
        {
            var pubs = await _db.PubLocations
                .Where(p => p.IsApproved)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Address,
                    p.Lat,
                    p.Lng,
                    p.IsOpen,
                    p.Rating,
                    p.Reviews,
                    p.Screens,
                    p.FreeEntry,
                    p.HdScreens,
                    p.ImageUrl
                })
                .ToListAsync();

            return Ok(pubs);
        }

        // ✅ POST: /api/pubs (Ajouter un pub)
        [HttpPost]
        [Route("api/pubs")]
        public async Task<IActionResult> AddPub([FromBody] AddPubRequest request)
        {
            var pub = new PubLocation
            {
                Name = request.Name,
                Address = request.Address,
                Lat = request.Lat,
                Lng = request.Lng,
                IsOpen = request.IsOpen,
                Rating = 0,
                Reviews = 0,
                Screens = request.Screens,
                FreeEntry = request.FreeEntry,
                HdScreens = request.HdScreens,
                IsApproved = false,
                ImageUrl = request.ImageUrl
            };

            _db.PubLocations.Add(pub);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, pubId = pub.Id });
        }

        // ✅ POST: /api/pubs/{id}/rate
        [HttpPost]
        [Route("api/pubs/{id}/rate")]
        public async Task<IActionResult> RatePub(int id, [FromBody] RatePubRequest request)
        {
            var pub = await _db.PubLocations.FindAsync(id);
            if (pub == null) return NotFound();

            var totalReviews = pub.Reviews + 1;
            var totalRating = pub.Rating * pub.Reviews + request.Rating;
            pub.Rating = totalRating / totalReviews;
            pub.Reviews = totalReviews;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, newRating = pub.Rating, totalReviews = pub.Reviews });
        }
    }

    public class AddPubRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public bool IsOpen { get; set; } = true;
        public int Screens { get; set; }
        public bool FreeEntry { get; set; }
        public bool HdScreens { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class RatePubRequest
    {
        public int Rating { get; set; }
    }
}