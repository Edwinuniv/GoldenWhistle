using GoldenWhistle.Data;
using GoldenWhistle.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Controllers
{
    public class KickoffController : Controller
    {
        private readonly ApplicationDbContext _db;

        public KickoffController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Grab live + upcoming matches — most relevant first
            var matches = await _db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.League)
                .Where(m => !m.Cancelled && !m.Finished)
                .OrderBy(m => m.Started ? 0 : 1)   // live first, then upcoming
                .ThenBy(m => m.KickoffUtc)
                .Take(10)
                .ToListAsync();

            var vm = new KickoffViewModel
            {
                Matches = matches.Select(m => BuildPreview(m)).ToList()
            };

            return View(vm);
        }

        // ── Build a realistic preview for any match ───────────────
        private static KickoffMatchViewModel BuildPreview(GoldenWhistle.Models.Match m)
        {
            var home = m.HomeTeam.Name;
            var away = m.AwayTeam.Name;

            return new KickoffMatchViewModel
            {
                MatchId = m.Id,
                HomeTeamName = home,
                HomeTeamCode = m.HomeTeam.ShortName,
                AwayTeamName = away,
                AwayTeamCode = m.AwayTeam.ShortName,
                KickoffUtc = m.KickoffUtc,
                StadiumInfo = $"{m.League.Name} · {m.KickoffUtc:HH:mm} UTC",

                HomeInjuries = GenerateInjuries(home),
                AwayInjuries = GenerateInjuries(away),

                HomeTactic = GenerateTactic(home),
                AwayTactic = GenerateTactic(away),

                Facts = GenerateFacts(home, away),
                H2H = GenerateH2H()
            };
        }

        // ── Injuries ──────────────────────────────────────────────
        private static List<InjuryItemViewModel> GenerateInjuries(string teamName)
        {
            // Seeded so the same team always gets the same stub data
            var seed = teamName.Length * 7 + teamName[0];
            var rng = new Random(seed);
            var count = rng.Next(1, 4);

            var players = new[]
            {
                ("Right Back",       "doubtful"),
                ("Centre Mid",       "out"),
                ("Left Winger",      "doubtful"),
                ("Striker",          "return"),
                ("Centre Back",      "out"),
                ("Goalkeeper",       "return"),
                ("Attacking Mid",    "doubtful"),
                ("Defensive Mid",    "out"),
            };

            var firstNames = new[] { "Marco", "Luca", "Kai", "Pablo", "Luis", "James", "Ali", "Diego" };
            var lastNames = new[] { "Rossi", "Müller", "García", "Silva", "Costa", "Petit", "Nzola", "Vargas" };

            return Enumerable.Range(0, count).Select(i =>
            {
                var (role, status) = players[(seed + i) % players.Length];
                var name = $"{firstNames[(seed + i * 3) % firstNames.Length]} " +
                           $"{lastNames[(seed + i * 5) % lastNames.Length]}";
                return new InjuryItemViewModel
                {
                    PlayerName = name,
                    Role = role,
                    Status = status
                };
            }).ToList();
        }

        // ── Tactics ───────────────────────────────────────────────
        private static TacticViewModel GenerateTactic(string teamName)
        {
            var seed = teamName.Length * 13 + teamName[^1];
            var formations = new[] { "4-3-3", "4-2-3-1", "3-5-2", "4-4-2", "5-3-2", "4-1-4-1" };
            var styles = new[]
            {
                "High press, fast transitions",
                "Possession-based build-up",
                "Deep block, counter-attack",
                "Gegenpressing with wide overloads",
                "Low block, set-piece threat"
            };
            var firstNames = new[] { "Luca", "Marco", "Kai", "Pablo", "James", "Ali" };
            var lastNames = new[] { "Rossi", "Silva", "Müller", "García", "Costa", "Nzola" };

            var rng = new Random(seed);
            var firstName = firstNames[seed % firstNames.Length];
            var lastName = lastNames[(seed * 3) % lastNames.Length];

            return new TacticViewModel
            {
                Formation = formations[seed % formations.Length],
                Style = styles[seed % styles.Length],
                KeyPlayer = $"{firstName} {lastName}",
                KeyPlayerInitial = firstName[0].ToString()
            };
        }

        // ── Weird facts ───────────────────────────────────────────
        private static List<FactViewModel> GenerateFacts(string home, string away)
        {
            var seed = (home.Length + away.Length) * 11;

            return new List<FactViewModel>
            {
                new()
                {
                    Emoji = "⚡",
                    Text  = $"{home} have won {3 + seed % 4} of their last 5 meetings",
                    Color = "green"
                },
                new()
                {
                    Emoji = "🎯",
                    Text  = $"{away} have scored in every away game this tournament",
                    Color = "blue"
                },
                new()
                {
                    Emoji = "🟨",
                    Text  = $"The referee averages {4 + seed % 3} yellow cards per match this tournament",
                    Color = "gold"
                },
                new()
                {
                    Emoji = "🔥",
                    Text  = $"{home}'s striker has scored in {2 + seed % 3} consecutive matches",
                    Color = "red"
                },
                new()
                {
                    Emoji = "🕐",
                    Text = $"{(seed % 2 == 0 ? home : away)} have conceded {1 + seed % 2} goals in the last 10 minutes of matches",
                    Color = "gold"
                }
            };
        }

        // ── H2H ───────────────────────────────────────────────────
        private static H2HViewModel GenerateH2H()
        {
            // Realistic-looking H2H over last 10 meetings
            return new H2HViewModel
            {
                HomeWins = 4,
                Draws = 2,
                AwayWins = 4,
                HomeGoals = 14,
                AwayGoals = 13
            };
        }
    }
}