using GoldenWhistle.Data;
using GoldenWhistle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var notifications = new List<object>();

            // 1. Match à venir (si dans les 2 heures)
            var upcomingMatch = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => !m.Started && !m.Cancelled && m.KickoffUtc > DateTime.UtcNow && m.KickoffUtc < DateTime.UtcNow.AddHours(2))
                .OrderBy(m => m.KickoffUtc)
                .FirstOrDefaultAsync();

            if (upcomingMatch != null)
            {
                notifications.Add(new
                {
                    id = 1,
                    type = "live",
                    icon = "⚽",
                    title = "Match Starting Soon",
                    message = $"{upcomingMatch.HomeTeam.Name} vs {upcomingMatch.AwayTeam.Name} kicks off in {Math.Round((upcomingMatch.KickoffUtc - DateTime.UtcNow).TotalMinutes)} minutes",
                    timeAgo = "Just now",
                    isRead = false
                });
            }

            // 2. Leaderboard update (si l'utilisateur a gagné des points)
            var recentPicks = await _db.BracketPicks
                .Where(p => p.UserId == userId && p.IsScored && p.ScoredAt > DateTime.UtcNow.AddHours(-24))
                .ToListAsync();

            if (recentPicks.Any())
            {
                var pointsGained = recentPicks.Sum(p => p.PointsAwarded);
                if (pointsGained > 0)
                {
                    notifications.Add(new
                    {
                        id = 2,
                        type = "anxious",
                        icon = "🏆",
                        title = "Points Earned!",
                        message = $"You earned {pointsGained} points from {recentPicks.Count} prediction{(recentPicks.Count > 1 ? "s" : "")}",
                        timeAgo = "Just now",
                        isRead = false
                    });
                }
            }

            // 3. Live matches en cours
            var liveMatches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Started && !m.Finished)
                .ToListAsync();

            foreach (var match in liveMatches)
            {
                notifications.Add(new
                {
                    id = 3 + match.Id,
                    type = "live",
                    icon = "🔴",
                    title = $"LIVE: {match.HomeTeam.Name} vs {match.AwayTeam.Name}",
                    message = $"Score: {match.HomeScore ?? 0}–{match.AwayScore ?? 0} · {match.StatusShort}",
                    timeAgo = "Live now",
                    isRead = false
                });
            }

            // Marquer comme lues
            notifications = notifications.OrderByDescending(n => n.GetType().GetProperty("id")?.GetValue(n)).ToList();

            return Ok(notifications);
        }
    }
}