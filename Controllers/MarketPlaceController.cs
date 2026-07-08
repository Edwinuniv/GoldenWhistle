using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels.Marketplace;
using Google.Protobuf;
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

        public IActionResult Index() => View(new MarketplaceViewModel());

        // ✅ GET: /api/listings
        [HttpGet]
        [Route("api/listings")]
        public async Task<IActionResult> GetListings(string filter = "all", int page = 1)
        {
            var query = _db.JerseyListings
                .Include(l => l.Seller)
                .Where(l => l.IsActive);

            // Appliquer les filtres
            if (filter == "bnwt") query = query.Where(l => l.Condition == "BNWT");
            else if (filter == "match") query = query.Where(l => l.Condition == "Match Worn");
            else if (filter == "player") query = query.Where(l => l.PlayerName.Contains("#"));
            else if (filter == "budget") query = query.Where(l => l.Price < 100);
            else if (filter == "auth") query = query.Where(l => l.IsVerified);

            var listings = await query
                .Skip((page - 1) * 12)
                .Take(12)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    Player = l.PlayerName,
                    l.Price,
                    l.Size,
                    l.Condition,
                    l.Tag,
                    Seller = l.Seller.DisplayName ?? l.Seller.UserName ?? "Unknown",
                    l.SellerRating,
                    l.IsVerified,
                    l.ImageUrl
                })
                .ToListAsync();

            return Ok(listings);
        }

        // ✅ POST: /api/listings (Créer une annonce)
        [HttpPost]
        [Authorize]
        [Route("api/listings")]
        public async Task<IActionResult> CreateListing([FromBody] CreateListingRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var listing = new JerseyListing
            {
                Title = request.Title,
                PlayerName = request.PlayerName,
                Price = request.Price,
                Size = request.Size,
                Condition = request.Condition,
                Tag = request.Tag,
                SellerId = userId,
                IsVerified = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = request.ImageUrl
            };

            _db.JerseyListings.Add(listing);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, listingId = listing.Id });
        }

        // ✅ POST: /api/messages
        [HttpPost]
        [Authorize]
        [Route("api/messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var message = new Message
            {
                SenderId = userId,
                ListingId = request.ListingId,
                Content = request.Message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class CreateListingRequest
    {
        public string Title { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class SendMessageRequest
    {
        public int ListingId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}