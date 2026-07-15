using GoldenWhistle.Data;
using GoldenWhistle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            return View(user);
        }

        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Update(string displayName, string country)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            user.DisplayName = displayName;
            user.Country = country;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Settings");
        }

        // NEW (audit §2): Profile/Index.cshtml's JS calls loadProfileStats()
        // -> GET /api/profile/stats on page load, but this endpoint never
        // existed, so "Predictions", "Accuracy", "Global Rank", etc. were
        // permanently stuck on "Loading..." / "--". Implemented using the
        // same real BracketPick data already used elsewhere (Dashboard,
        // Bracket).
        [HttpGet]
        [Route("api/profile/stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized();

            var scoredPicks = await _db.BracketPicks
                .Where(p => p.UserId == userId && p.IsScored)
                .ToListAsync();

            var totalPicks = scoredPicks.Count;
            var correctPicks = scoredPicks.Count(p => p.PointsAwarded > 0);
            var accuracy = totalPicks > 0 ? (int)Math.Round(correctPicks * 100.0 / totalPicks) : 0;

            var rankedUserIds = await _db.Users
                .OrderByDescending(u => u.TotalPoints)
                .Select(u => u.Id)
                .ToListAsync();

            var rank = rankedUserIds.IndexOf(userId) + 1;

            return Ok(new
            {
                totalPicks,
                correctPicks,
                accuracy,
                rank = rank > 0 ? rank : 0
            });
        }
    }
}
