using GoldenWhistle.Data;
using GoldenWhistle.Hubs;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels.Mood;
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
        private readonly ILogger<MoodController> _logger;

        public MoodController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHubContext<MoodMapHub> moodHub,
            ILogger<MoodController> logger)
        {
            _db = db;
            _userManager = userManager;
            _moodHub = moodHub;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
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
                {
                    return View(new MoodViewModel
                    {
                        MatchId = 0,
                        HomeTeamName = "No Match",
                        AwayTeamName = "Available Soon",
                        MatchMinuteLabel = "Upcoming",
                        ScoreLabel = "0–0",
                        TotalVotes = 0,
                        EcstasyCount = 0,
                        AnxietyCount = 0,
                        AgonyCount = 0,
                        EcstasyPct = 0,
                        AnxietyPct = 0,
                        AgonyPct = 0,
                        CurrentUserVote = null,
                        Timeline = new List<MoodTimelinePoint>()
                    });
                }

                var votes = await _db.MoodVotes
                    .Where(v => v.MatchId == liveMatch.Id)
                    .ToListAsync();

                var totalVotes = votes.Count;
                var ecstasyCount = votes.Count(v => v.Mood == MoodType.Ecstasy);
                var anxietyCount = votes.Count(v => v.Mood == MoodType.Anxiety);
                var agonyCount = votes.Count(v => v.Mood == MoodType.Agony);

                var ecstasyPct = totalVotes > 0 ? (int)Math.Round(ecstasyCount * 100.0 / totalVotes) : 0;
                var anxietyPct = totalVotes > 0 ? (int)Math.Round(anxietyCount * 100.0 / totalVotes) : 0;
                var agonyPct = totalVotes > 0 ? (int)Math.Round(agonyCount * 100.0 / totalVotes) : 0;

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

                // NOTE: unlike the previous version, we no longer pad the
                // timeline with 8 fabricated zero-value points when there is
                // no vote history yet — an empty list is the honest
                // representation of "no data collected", and the chart
                // renders an empty axis instead of implying false precision.

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
                    EcstasyPct = ecstasyPct,
                    AnxietyPct = anxietyPct,
                    AgonyPct = agonyPct,
                    CurrentUserVote = currentUserVote,
                    Timeline = timeline
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Mood page");
                return View(new MoodViewModel
                {
                    MatchId = 0,
                    HomeTeamName = "Error",
                    AwayTeamName = "Loading",
                    MatchMinuteLabel = "Error",
                    ScoreLabel = "0–0",
                    TotalVotes = 0,
                    EcstasyCount = 0,
                    AnxietyCount = 0,
                    AgonyCount = 0,
                    EcstasyPct = 0,
                    AnxietyPct = 0,
                    AgonyPct = 0,
                    CurrentUserVote = null,
                    Timeline = new List<MoodTimelinePoint>()
                });
            }
        }

        // NEW (audit §2): site.js's loadMatches() called this route to
        // populate the "-- Select a match --" dropdown on the Mood page, but
        // it never existed server-side, so the selector was always empty.
        [HttpGet]
        [Route("api/mood/matches")]
        public async Task<IActionResult> GetVotableMatches()
        {
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => !m.Cancelled)
                .OrderByDescending(m => m.Started && !m.Finished) // live first
                .ThenBy(m => m.KickoffUtc)
                .Take(20)
                .Select(m => new
                {
                    id = m.Id,
                    homeTeam = m.HomeTeam.Name,
                    awayTeam = m.AwayTeam.Name,
                    date = m.KickoffUtc.ToString("MMM dd, HH:mm")
                })
                .ToListAsync();

            return Ok(matches);
        }

        [HttpPost]
        [Authorize]
        [Route("api/mood/vote")]
        public async Task<IActionResult> Vote([FromBody] VoteRequest request)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                if (request == null || request.MatchId <= 0)
                {
                    return BadRequest(new { error = "Invalid match ID" });
                }

                if (!Enum.TryParse<MoodType>(request.Mood, true, out var moodType))
                {
                    return BadRequest(new { error = "Invalid mood type. Use: Ecstasy, Anxiety, Agony" });
                }

                var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == request.MatchId);
                if (match == null)
                {
                    return NotFound(new { error = "Match not found" });
                }

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

                var votes = await _db.MoodVotes
                    .Where(v => v.MatchId == request.MatchId)
                    .ToListAsync();

                var total = votes.Count;
                var ecstasy = votes.Count(v => v.Mood == MoodType.Ecstasy);
                var anxiety = votes.Count(v => v.Mood == MoodType.Anxiety);
                var agony = votes.Count(v => v.Mood == MoodType.Agony);

                var ecstasyPct = total > 0 ? (int)Math.Round(ecstasy * 100.0 / total) : 0;
                var anxietyPct = total > 0 ? (int)Math.Round(anxiety * 100.0 / total) : 0;
                var agonyPct = total > 0 ? (int)Math.Round(agony * 100.0 / total) : 0;

                try
                {
                    await _moodHub.Clients.All.SendAsync("ReceiveTallies", new
                    {
                        apiMatchId = match.ApiMatchId,
                        ecstasy = ecstasyPct,
                        anxiety = anxietyPct,
                        agony = agonyPct,
                        total = total
                    });
                }
                catch (Exception signalREx)
                {
                    _logger.LogWarning(signalREx, "SignalR broadcast failed, but vote was saved");
                }

                return Ok(new
                {
                    ecstasyPct,
                    anxietyPct,
                    agonyPct,
                    totalVotes = total,
                    ecstasyCount = ecstasy,
                    anxietyCount = anxiety,
                    agonyCount = agony
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vote");
                return StatusCode(500, new { error = "An error occurred while processing your vote" });
            }
        }

        // REMOVED (audit §1 & §5, critical): api/mood/test-vote used to have
        // no [Authorize] attribute and would insert a real MoodVote row tied
        // to a fresh random Guid "test-user", directly skewing the live mood
        // percentages shown to every real user on the Dashboard and Mood
        // page. Anyone on the internet could call it repeatedly. This is
        // exactly the kind of fake-data injection the audit flagged, so the
        // endpoint has been deleted rather than merely secured — there is no
        // legitimate production use for it. If you need a way to seed mood
        // data for local development/demos, do it via a database seeding
        // script gated behind `app.Environment.IsDevelopment()`, never as a
        // reachable HTTP endpoint.

        [HttpGet]
        [Route("api/mood/stats/{matchId}")]
        public async Task<IActionResult> GetStats(int matchId)
        {
            try
            {
                var match = await _db.Matches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .FirstOrDefaultAsync(m => m.Id == matchId);

                if (match == null)
                {
                    return NotFound("Match not found");
                }

                var votes = await _db.MoodVotes
                    .Where(v => v.MatchId == matchId)
                    .ToListAsync();

                var total = votes.Count;
                var ecstasy = votes.Count(v => v.Mood == MoodType.Ecstasy);
                var anxiety = votes.Count(v => v.Mood == MoodType.Anxiety);
                var agony = votes.Count(v => v.Mood == MoodType.Agony);

                var ecstasyPct = total > 0 ? (int)Math.Round(ecstasy * 100.0 / total) : 0;
                var anxietyPct = total > 0 ? (int)Math.Round(anxiety * 100.0 / total) : 0;
                var agonyPct = total > 0 ? (int)Math.Round(agony * 100.0 / total) : 0;

                return Ok(new
                {
                    matchId = match.Id,
                    homeTeam = match.HomeTeam.Name,
                    awayTeam = match.AwayTeam.Name,
                    status = match.Started ? (match.Finished ? "Full-time" : "Live") : "Pre-match",
                    score = $"{match.HomeScore ?? 0}–{match.AwayScore ?? 0}",
                    totalVotes = total,
                    ecstasy,
                    anxiety,
                    agony,
                    ecstasyPct,
                    anxietyPct,
                    agonyPct
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mood stats");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/mood/global-stats")]
        public async Task<IActionResult> GetGlobalStats()
        {
            var votes = await _db.MoodVotes
                .Where(v => v.Match != null && (v.Match.Started || v.Match.Finished))
                .ToListAsync();

            var total = votes.Count;
            if (total == 0)
            {
                return Ok(new { ecstasy = 0, anxious = 0, agony = 0, total = 0 });
            }

            var ecstasy = (int)Math.Round(votes.Count(v => v.Mood == MoodType.Ecstasy) * 100.0 / total);
            var anxious = (int)Math.Round(votes.Count(v => v.Mood == MoodType.Anxiety) * 100.0 / total);
            var agony = (int)Math.Round(votes.Count(v => v.Mood == MoodType.Agony) * 100.0 / total);

            return Ok(new { ecstasy, anxious, agony, total });
        }
    }

    public class VoteRequest
    {
        public int MatchId { get; set; }
        public string Mood { get; set; } = string.Empty;
    }
}
