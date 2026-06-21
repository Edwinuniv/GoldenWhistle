using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class MarketplaceController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MarketplaceController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Page — JS loads listings via /api/listings
        public IActionResult Index() => View(new MarketplaceViewModel());

        [HttpGet]
        [Route("api/listings")]
        public async Task<IActionResult> GetListings(string filter = "all", int page = 1)
        {
            // TODO: query JerseyListings table once Dev A creates it
            // var listings = await _db.JerseyListings
            //     .Include(l => l.Seller)
            //     .Where(l => l.IsActive)
            //     .Skip((page - 1) * 12).Take(12)
            //     .ToListAsync();
            return Ok(new List<object>());
        }

        [HttpPost]
        [Authorize]
        [Route("api/messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // TODO: save to Messages table once Dev A creates it
            // _db.Messages.Add(new Message {
            //     SenderId   = userId,
            //     ListingId  = request.ListingId,
            //     Content    = request.Message,
            //     SentAt     = DateTime.UtcNow
            // });
            // await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class SendMessageRequest
    {
        public int ListingId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}