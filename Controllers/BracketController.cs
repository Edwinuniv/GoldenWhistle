using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    [Authorize]
    public class BracketController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public BracketController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.League)
                .OrderBy(m => m.KickoffUtc)
                .ToListAsync();

            var userPicks = await _db.BracketPicks
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var picksByMatchId = userPicks.ToDictionary(p => p.MatchId);

            var membership = await _db.LeagueMembers
                .Include(m => m.League)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            List<LeagueStandingViewModel> leagueStandings = new();
            if (membership is not null)
            {
                var members = await _db.LeagueMembers
                    .Include(m => m.User)
                    .Where(m => m.PrivateLeagueId == membership.PrivateLeagueId)
                    .OrderByDescending(m => m.User.TotalPoints)
                    .ToListAsync();

                leagueStandings = members.Select((m, i) => new LeagueStandingViewModel
                {
                    Rank = i + 1,
                    UserName = m.User.DisplayName ?? m.User.UserName ?? "Fan",
                    CorrectPicks = userPicks.Count(p => p.IsScored && p.PointsAwarded > 0),
                    Points = m.User.TotalPoints
                }).ToList();
            }

            var totalCorrect = userPicks.Count(p => p.IsScored && p.PointsAwarded > 0);
            var totalPending = userPicks.Count(p => !p.IsScored && !p.IsLocked);

            var vm = new BracketViewModel
            {
                TotalCorrect = totalCorrect,
                TotalPending = totalPending,
                LeagueName = membership?.League.Name ?? "No league yet",

                Picks = matches.Select(m =>
                {
                    picksByMatchId.TryGetValue(m.Id, out var pick);
                    return new BracketMatchViewModel
                    {
                        MatchId = m.Id,
                        Round = m.League.Name,
                        HomeTeamCode = m.HomeTeam.ShortName,
                        HomeTeamName = m.HomeTeam.Name,
                        AwayTeamCode = m.AwayTeam.ShortName,
                        AwayTeamName = m.AwayTeam.Name,
                        HomeScore = m.HomeScore,
                        AwayScore = m.AwayScore,
                        KickoffTime = m.KickoffUtc.ToLocalTime().ToString("HH:mm"),
                        IsLive = m.Started && !m.Finished,
                        IsWinner = m.Finished && m.HomeScore > m.AwayScore,
                        UserPick = pick?.PredictedOutcome.ToString(),
                        PointsAwarded = pick?.PointsAwarded ?? 0,
                        IsScored = pick?.IsScored ?? false,
                        IsLocked = pick?.IsLocked ?? false,
                    };
                }).ToList(),

                LeagueStandings = leagueStandings,
                LiveEvents = new List<LiveEventViewModel>()
            };

            return View(vm);
        }

        [HttpPost]
        [Route("api/bracket/pick")]
        public async Task<IActionResult> SubmitPick([FromBody] BracketPickRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized();

            var match = await _db.Matches.FindAsync(request.MatchId);
            if (match is null) return NotFound("Match not found.");
            if (match.Started) return BadRequest("Match has already started — picks are locked.");

            if (!Enum.TryParse<PickOutcome>(request.PredictedOutcome, true, out var outcome))
                return BadRequest("Invalid outcome. Use Home, Away, or Draw.");

            var existing = await _db.BracketPicks
                .FirstOrDefaultAsync(p => p.MatchId == request.MatchId && p.UserId == userId);

            if (existing is null)
            {
                existing = new BracketPick
                {
                    UserId = userId,
                    MatchId = request.MatchId,
                    CreatedAt = DateTime.UtcNow
                };
                _db.BracketPicks.Add(existing);
            }

            existing.PredictedOutcome = outcome;
            existing.PredictedHomeScore = request.PredictedHomeScore;
            existing.PredictedAwayScore = request.PredictedAwayScore;

            existing.PredictedFirstScorerName = request.PredictedFirstScorerName;
            existing.PredictedLastScorerName = request.PredictedLastScorerName;
            existing.PredictedAnytimeScorerName = request.PredictedAnytimeScorerName;
            existing.PredictedOwnGoal = request.PredictedOwnGoal;
            existing.PredictedOwnGoalTeamId = request.PredictedOwnGoalTeamId;

            existing.PredictedMostAssistsPlayerName = request.PredictedMostAssistsPlayerName;
            existing.PredictedManOfTheMatchName = request.PredictedManOfTheMatchName;

            existing.PredictedMostYellowsTeamId = request.PredictedMostYellowsTeamId;
            existing.PredictedMostRedsTeamId = request.PredictedMostRedsTeamId;
            existing.PredictedMostFoulsTeamId = request.PredictedMostFoulsTeamId;
            existing.PredictedMostFoulsPlayerName = request.PredictedMostFoulsPlayerName;

            existing.PredictedMostCornersTeamId = request.PredictedMostCornersTeamId;

            existing.PredictedBetterPossessionTeamId = request.PredictedBetterPossessionTeamId;
            existing.PredictedMostPassesTeamId = request.PredictedMostPassesTeamId;
            existing.PredictedMostPassesPlayerName = request.PredictedMostPassesPlayerName;

            existing.PredictedHigherXgTeamId = request.PredictedHigherXgTeamId;

            existing.PredictedMostSavesGoalkeeperName = request.PredictedMostSavesGoalkeeperName;
            existing.PredictedMostSavesTeamId = request.PredictedMostSavesTeamId;

            existing.PredictedMostDistancePlayerName = request.PredictedMostDistancePlayerName;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Pick saved.", matchId = request.MatchId });
        }

        [HttpGet]
        [Route("api/bracket/picks")]
        public async Task<IActionResult> GetMyPicks()
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null) return Unauthorized();

            var picks = await _db.BracketPicks
                .Where(p => p.UserId == userId)
                .Select(p => new
                {
                    p.MatchId,
                    p.PredictedOutcome,
                    p.PredictedHomeScore,
                    p.PredictedAwayScore,
                    p.IsLocked,
                    p.IsScored,
                    p.PointsAwarded,
                    p.IsUpset
                })
                .ToListAsync();

            return Ok(picks);
        }

        [HttpGet]
        [Route("api/bracket/leaderboard")]
        public async Task<IActionResult> GlobalLeaderboard(int page = 1, int pageSize = 20)
        {
            var users = await _db.Users
                .OrderByDescending(u => u.TotalPoints)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select((u, i) => new
                {
                    UserName = u.DisplayName ?? u.UserName ?? "Fan",
                    u.TotalPoints,
                    u.Country
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}