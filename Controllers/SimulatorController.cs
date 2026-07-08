using GoldenWhistle.Data;
using GoldenWhistle.Services.Interfaces;
using GoldenWhistle.ViewModels.Shared;
using GoldenWhistle.ViewModels.Simulator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class SimulatorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IChatService _chatService;
        private readonly ILogger<SimulatorController> _logger;

        public SimulatorController(
            ApplicationDbContext db,
            IChatService chatService,
            ILogger<SimulatorController> logger)
        {
            _db = db;
            _chatService = chatService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Finished || m.Started)
                .OrderBy(m => m.KickoffUtc)
                .Take(8)
                .ToListAsync();

            var vm = new SimulatorViewModel
            {
                Matches = matches.Select(m => new SimMatchViewModel
                {
                    MatchId = m.Id,
                    HomeTeamName = m.HomeTeam.Name,
                    HomeTeamCode = m.HomeTeam.ShortName,
                    AwayTeamName = m.AwayTeam.Name,
                    AwayTeamCode = m.AwayTeam.ShortName,
                    HomeScore = m.HomeScore ?? 0,
                    AwayScore = m.AwayScore ?? 0
                }).ToList()
            };

            return View(vm);
        }

        // ✅ GET: /api/simulator/matches
        [HttpGet]
        [Route("api/simulator/matches")]
        public async Task<IActionResult> GetMatches()
        {
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Finished || m.Started)
                .OrderBy(m => m.KickoffUtc)
                .Take(8)
                .Select(m => new
                {
                    matchId = m.Id,
                    homeTeamName = m.HomeTeam.Name,
                    homeTeamCode = m.HomeTeam.ShortName,
                    awayTeamName = m.AwayTeam.Name,
                    awayTeamCode = m.AwayTeam.ShortName,
                    homeScore = m.HomeScore ?? 0,
                    awayScore = m.AwayScore ?? 0
                })
                .ToListAsync();

            return Ok(matches);
        }

        // ✅ POST: /api/simulator/run - AVEC GEMINI
        [HttpPost]
        [Route("api/simulator/run")]
        public async Task<IActionResult> Run([FromBody] SimulatorRunRequest request)
        {
            try
            {
                var winners = request.Matches.Select(m => new
                {
                    m.MatchId,
                    Winner = m.HomeScore > m.AwayScore ? "home" :
                             m.AwayScore > m.HomeScore ? "away" : "draw",
                    IsUpset = m.HomeScore > m.AwayScore && m.HomeScore - m.AwayScore <= 1
                }).ToList();

                // ✅ Générer une narrative avec Gemini
                var prompt = BuildNarrativePrompt(request.Matches);
                var narrative = await _chatService.GetChatResponseAsync(prompt);

                return Ok(new
                {
                    winners,
                    narrative,
                    winProbabilities = CalculateWinProbabilities(request.Matches)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulator run failed");
                // Fallback narrative
                var fallback = "In this alternate timeline, the results rewrote history...";
                return Ok(new
                {
                    winners = request.Matches.Select(m => new
                    {
                        m.MatchId,
                        Winner = m.HomeScore > m.AwayScore ? "home" :
                                 m.AwayScore > m.HomeScore ? "away" : "draw",
                        IsUpset = false
                    }),
                    narrative = fallback,
                    winProbabilities = new[]
                    {
                        new { team = "TBD", probability = 50 },
                        new { team = "TBD", probability = 50 }
                    }
                });
            }
        }

        private string BuildNarrativePrompt(List<SimMatchScore> matches)
        {
            var matchDescriptions = string.Join("\n", matches.Select((m, i) =>
                $"Match {i + 1}: Home {m.HomeScore} - {m.AwayScore} Away"));

            return $@"You are a sports commentator for GoldenWhistle.

Based on these hypothetical match results:
{matchDescriptions}

Write a short, exciting narrative (3-4 sentences) about what happened in this alternate timeline.
Include which team caused the biggest surprise and who reached the final.
Be dramatic and engaging, like a sports broadcaster.";
        }

        private object[] CalculateWinProbabilities(List<SimMatchScore> matches)
        {
            if (matches.Count < 4) return new[]
            {
                new { team = "TBD", probability = 50 },
                new { team = "TBD", probability = 50 }
            };

            // Simple simulation: top 2 scorers go to final
            var topTeams = matches
                .SelectMany(m => new[]
                {
                    new { Team = m.HomeTeamId.ToString(), Score = m.HomeScore },
                    new { Team = m.AwayTeamId.ToString(), Score = m.AwayScore }
                })
                .OrderByDescending(x => x.Score)
                .Take(2)
                .ToList();

            if (topTeams.Count < 2) return new[]
            {
                new { team = "TBD", probability = 50 },
                new { team = "TBD", probability = 50 }
            };

            var totalScore = topTeams.Sum(x => x.Score);
            if (totalScore == 0) totalScore = 1;

            return topTeams.Select(t => new
            {
                team = $"Team {t.Team}",
                probability = (int)Math.Round(t.Score * 100.0 / totalScore)
            }).ToArray();
        }
    }

    public class SimulatorRunRequest
    {
        public List<SimMatchScore> Matches { get; set; } = new();
    }

    public class SimMatchScore
    {
        public int MatchId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
    }
}