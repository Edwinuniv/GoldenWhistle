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

        public MoodController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHubContext<MoodMapHub> moodHub)
        {
            _db = db;
            _userManager = userManager;
            _moodHub = moodHub;
        }

        public async Task<IActionResult> Index()
        {
            var liveMatch = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .FirstOrDefaultAsync(m => m.Started && !m.Finished);

            if (liveMatch == null)
            {
                liveMatch = await _db.Matches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => !m.Started && !m.Cancelled)
                    .OrderBy(m => m.KickoffUtc)
                    .FirstOrDefaultAsync();
            }

            if (liveMatch == null)
                return View(new MoodViewModel());

            var votes = await _db.MoodVotes
                .Where(v => v.MatchId == liveMatch.Id)
                .ToListAsync();

            var totalVotes = votes.Count;
            var ecstasyCount = votes.Count(v => v.Mood == MoodType.Ecstasy);
            var anxietyCount = votes.Count(v => v.Mood == MoodType.Anxiety);
            var agonyCount = votes.Count(v => v.Mood == MoodType.Agony);

            var userId = _userManager.GetUserId(User);
            string? currentUserVote = null;
            if (!string.IsNullOrEmpty(userId))
            {
                var userVote = votes.FirstOrDefault(v => v.UserId == userId);
                currentUserVote = userVote?.Mood.ToString();
            }

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
                CurrentUserVote = currentUserVote,
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

            if (!Enum.TryParse<MoodType>(request.Mood, true, out var moodType))
                return BadRequest("Invalid mood type. Use: Ecstasy, Anxiety, Agony");

            var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == request.MatchId);
            if (match == null) return NotFound("Match not found.");

            var existing = await _db.MoodVotes
                .FirstOrDefaultAsync(v => v.MatchId == request.MatchId && v.UserId == userId);

            if (existing is null)
            {
                _db.MoodVotes.Add(new MoodVote
                {
                    MatchId = request.MatchId,
                    UserId = userId,
                    Mood = moodType,
                    VotedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Mood = moodType;
                existing.VotedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            var votes = await _db.MoodVotes.Where(v => v.MatchId == request.MatchId).ToListAsync();
            var total = votes.Count;
            var ecstasy = votes.Count(v => v.Mood == MoodType.Ecstasy);
            var agony = votes.Count(v => v.Mood == MoodType.Agony);
            var anxiety = votes.Count(v => v.Mood == MoodType.Anxiety);

            await _moodHub.Clients.All.SendAsync("ReceiveTallies", new
            {
                apiMatchId = match.ApiMatchId,
                ecstasy,
                agony,
                anxiety,
                total
            });

            return Ok(new
            {
                ecstasyPct = total > 0 ? (int)Math.Round(ecstasy * 100.0 / total) : 0,
                anxietyPct = total > 0 ? (int)Math.Round(anxiety * 100.0 / total) : 0,
                agonyPct = total > 0 ? (int)Math.Round(agony * 100.0 / total) : 0,
                totalVotes = total
            });
        }

        public class VoteRequest
        {
            public int MatchId { get; set; }
            public string Mood { get; set; } = string.Empty;
        }
    }
}