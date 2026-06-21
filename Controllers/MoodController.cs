using GoldenWhistle.Data;
using GoldenWhistle.Hubs;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class MoodController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<MoodMapHub> _moodHub;

        public MoodController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IHubContext<MoodMapHub> moodHub)
        {
            _db = db;
            _userManager = userManager;
            _moodHub = moodHub;
        }

        public async Task<IActionResult> Index()
        {
            // Get the current live match
            var liveMatch = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .FirstOrDefaultAsync(m => m.Started && !m.Finished);

            if (liveMatch == null)
            {
                // Fallback to next upcoming match
                liveMatch = await _db.Matches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => !m.Started)
                    .OrderBy(m => m.KickoffUtc)
                    .FirstOrDefaultAsync();
            }

            if (liveMatch == null) return View(new MoodViewModel());

            // Get all votes for this match
            var votes = await _db.MoodVotes
                .Where(v => v.MatchId == liveMatch.Id)
                .ToListAsync();

            var totalVotes = votes.Count;
            var ecstasyCount = votes.Count(v => v.Mood == MoodType.Ecstasy);
            var anxietyCount = votes.Count(v => v.Mood == MoodType.Anxiety);
            var agonyCount = votes.Count(v => v.Mood == MoodType.Agony);

            // Current user's vote
            var userId = _userManager.GetUserId(User);
            var userVote = votes.FirstOrDefault(v => v.UserId == userId);

            // Timeline: group votes by 15-min intervals
            var timeline = votes
                .GroupBy(v => (int)((v.VotedAt - liveMatch.KickoffUtc).TotalMinutes / 15) * 15)
                .OrderBy(g => g.Key)
                .Select(g => new MoodTimelinePoint
                {
                    Minute = $"{g.Key}'",
                    EcstasyCount = g.Count(v => v.Mood == MoodType.Ecstasy),
                    AnxietyCount = g.Count(v => v.Mood == MoodType.Anxiety),
                    AgonyCount = g.Count(v => v.Mood == MoodType.Agony)
                }).ToList();

            var vm = new MoodViewModel
            {
                MatchId = liveMatch.Id,
                HomeTeamName = liveMatch.HomeTeam.Name,
                AwayTeamName = liveMatch.AwayTeam.Name,
                MatchMinuteLabel = liveMatch.Started ? liveMatch.StatusShort : "Pre-match",
                ScoreLabel = $"{liveMatch.HomeScore ?? 0}–{liveMatch.AwayScore ?? 0}",
                TotalVotes = totalVotes,
                EcstasyCount = ecstasyCount,
                AnxietyCount = anxietyCount,
                AgonyCount = agonyCount,
                EcstasyPct = totalVotes > 0 ? (int)Math.Round(ecstasyCount * 100.0 / totalVotes) : 0,
                AnxietyPct = totalVotes > 0 ? (int)Math.Round(anxietyCount * 100.0 / totalVotes) : 0,
                AgonyPct = totalVotes > 0 ? (int)Math.Round(agonyCount * 100.0 / totalVotes) : 0,
                CurrentUserVote = userVote?.Mood.ToString(),
                Timeline = timeline
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [Route("api/mood/vote")]
        public async Task<IActionResult> Vote([FromBody] VoteRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Parse mood type
            if (!Enum.TryParse<MoodType>(request.Mood, true, out var moodType))
                return BadRequest("Invalid mood type. Use: Ecstasy, Anxiety, Agony");

            // Remove existing vote for this match
            var existing = await _db.MoodVotes
                .FirstOrDefaultAsync(v => v.MatchId == request.MatchId && v.UserId == userId);

            if (existing != null) _db.MoodVotes.Remove(existing);

            // Add new vote
            _db.MoodVotes.Add(new MoodVote
            {
                MatchId = request.MatchId,
                UserId = userId,
                Mood = moodType,
                VotedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Recalculate percentages
            var votes = await _db.MoodVotes.Where(v => v.MatchId == request.MatchId).ToListAsync();
            var total = votes.Count;
            var ecstasy = total > 0 ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Ecstasy) * 100.0 / total) : 0;
            var anxiety = total > 0 ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Anxiety) * 100.0 / total) : 0;
            var agony = total > 0 ? (int)Math.Round(votes.Count(v => v.Mood == MoodType.Agony) * 100.0 / total) : 0;

            // Push update to ALL connected clients via SignalR
            await _moodHub.Clients.All.SendAsync("ReceiveMoodUpdate",
                request.MatchId, moodType.ToString(), ecstasy, agony, anxiety);

            return Ok(new { ecstasyPct = ecstasy, anxietyPct = anxiety, agonyPct = agony, totalVotes = total });
        }
    }

    public class VoteRequest
    {
        public int MatchId { get; set; }
        public string Mood { get; set; } = string.Empty;
    }
}