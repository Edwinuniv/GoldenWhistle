using GoldenWhistle.Data;
using GoldenWhistle.Models;
using GoldenWhistle.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Services;

public class BracketScoringService : IBracketScoringService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BracketScoringService> _logger;

    // ── Point values ──────────────────────────────────────────────
    private const int PtsCorrectOutcome = 10;
    private const int PtsCorrectExactScore = 15;
    private const int PtsCorrectFirstScorer = 20;
    private const int PtsCorrectLastScorer = 15;
    private const int PtsCorrectAnytimeScorer = 10;
    private const int PtsCorrectOwnGoal = 10;
    private const int PtsCorrectManOfTheMatch = 15;
    private const int PtsCorrectMostAssists = 12;
    private const int PtsCorrectMostFoulsPlayer = 8;
    private const int PtsCorrectMostFoulsTeam = 5;
    private const int PtsCorrectYellowsTeam = 5;
    private const int PtsCorrectRedsTeam = 8;
    private const int PtsCorrectCornersTeam = 5;
    private const int PtsCorrectPossessionTeam = 5;
    private const int PtsCorrectPassesTeam = 5;
    private const int PtsCorrectPassesPlayer = 8;
    private const int PtsCorrectXgTeam = 5;
    private const int PtsCorrectMostSavesKeeper = 10;
    private const int PtsCorrectMostSavesTeam = 5;
    private const int PtsCorrectMostDistPlayer = 8;
    private const double UpsetMultiplier = 1.5;

    public BracketScoringService(
        ApplicationDbContext db,
        ILogger<BracketScoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> ScoreFinishedMatchesAsync()
    {
        var finishedMatchIds = await _db.Matches
            .Where(m => m.Finished)
            .Select(m => m.Id)
            .ToListAsync();

        var unscoredPicks = await _db.BracketPicks
            .Include(p => p.Match)
            .Include(p => p.User)
            .Where(p => finishedMatchIds.Contains(p.MatchId) && !p.IsScored)
            .ToListAsync();

        if (unscoredPicks.Count == 0)
        {
            _logger.LogInformation("No unscored picks found.");
            return 0;
        }

        var matchIds = unscoredPicks.Select(p => p.MatchId).Distinct().ToList();
        var stats = await _db.MatchStats
            .Where(s => matchIds.Contains(s.MatchId))
            .ToListAsync();
        var statsByMatchId = stats.ToDictionary(s => s.MatchId);

        int totalScored = 0;

        foreach (var pick in unscoredPicks)
        {
            var match = pick.Match;
            statsByMatchId.TryGetValue(match.Id, out var s);

            int points = 0;

            // ── 1. Outcome ────────────────────────────────────────
            var actualOutcome = GetActualOutcome(match);
            bool correctOutcome = pick.PredictedOutcome == actualOutcome;
            if (correctOutcome)
                points += PtsCorrectOutcome;

            // ── 2. Exact scoreline ────────────────────────────────
            if (pick.PredictedHomeScore.HasValue && pick.PredictedAwayScore.HasValue)
            {
                if (pick.PredictedHomeScore == match.HomeScore &&
                    pick.PredictedAwayScore == match.AwayScore)
                    points += PtsCorrectExactScore;
            }

            // ── 3. First scorer ───────────────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedFirstScorerName) && s is not null)
            {
                if (Eq(pick.PredictedFirstScorerName, s.FirstScorerName))
                    points += PtsCorrectFirstScorer;
            }

            // ── 4. Last scorer ────────────────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedLastScorerName) && s is not null)
            {
                if (Eq(pick.PredictedLastScorerName, s.LastScorerName))
                    points += PtsCorrectLastScorer;
            }

            // ── 5. Anytime scorer ─────────────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedAnytimeScorerName) && s is not null)
            {
                if (s.GoalScorerNames.Any(n => Eq(n, pick.PredictedAnytimeScorerName)))
                    points += PtsCorrectAnytimeScorer;
            }

            // ── 6. Own goal ───────────────────────────────────────
            if (pick.PredictedOwnGoal && s is not null)
            {
                if (s.OwnGoalScorerNames.Count > 0)
                    points += PtsCorrectOwnGoal;
            }

            // ── 7. Man of the Match ───────────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedManOfTheMatchName) && s is not null)
            {
                if (Eq(pick.PredictedManOfTheMatchName, s.ManOfTheMatchName))
                    points += PtsCorrectManOfTheMatch;
            }

            // ── 8. Most assists player ────────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedMostAssistsPlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostAssistsPlayerName, s.MostAssistsPlayerName))
                    points += PtsCorrectMostAssists;
            }

            // ── 9. Most fouls player ──────────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedMostFoulsPlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostFoulsPlayerName, s.MostFoulsPlayerName))
                    points += PtsCorrectMostFoulsPlayer;
            }

            // ── 10. Most fouls team ───────────────────────────────
            if (pick.PredictedMostFoulsTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostFoulsTeamId == s.MostFoulsTeamId)
                    points += PtsCorrectMostFoulsTeam;
            }

            // ── 11. Most yellows team ─────────────────────────────
            if (pick.PredictedMostYellowsTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostYellowsTeamId == s.MostYellowsTeamId)
                    points += PtsCorrectYellowsTeam;
            }

            // ── 12. Most reds team ────────────────────────────────
            if (pick.PredictedMostRedsTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostRedsTeamId == s.MostRedsTeamId)
                    points += PtsCorrectRedsTeam;
            }

            // ── 13. Most corners team ─────────────────────────────
            if (pick.PredictedMostCornersTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostCornersTeamId == s.MostCornersTeamId)
                    points += PtsCorrectCornersTeam;
            }

            // ── 14. Better possession team ────────────────────────
            if (pick.PredictedBetterPossessionTeamId.HasValue && s is not null)
            {
                if (pick.PredictedBetterPossessionTeamId == s.BetterPossessionTeamId)
                    points += PtsCorrectPossessionTeam;
            }

            // ── 15. Most passes team ──────────────────────────────
            if (pick.PredictedMostPassesTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostPassesTeamId == s.MostPassesTeamId)
                    points += PtsCorrectPassesTeam;
            }

            // ── 16. Most passes player ────────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedMostPassesPlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostPassesPlayerName, s.MostPassesPlayerName))
                    points += PtsCorrectPassesPlayer;
            }

            // ── 17. Higher xG team ────────────────────────────────
            if (pick.PredictedHigherXgTeamId.HasValue && s is not null)
            {
                if (pick.PredictedHigherXgTeamId == s.HigherXgTeamId)
                    points += PtsCorrectXgTeam;
            }

            // ── 18. Most saves goalkeeper ─────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedMostSavesGoalkeeperName) && s is not null)
            {
                if (Eq(pick.PredictedMostSavesGoalkeeperName, s.MostSavesGoalkeeperName))
                    points += PtsCorrectMostSavesKeeper;
            }

            // ── 19. Most saves team ───────────────────────────────
            if (pick.PredictedMostSavesTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostSavesTeamId == s.MostSavesTeamId)
                    points += PtsCorrectMostSavesTeam;
            }

            // ── 20. Most distance player ──────────────────────────
            if (!string.IsNullOrEmpty(pick.PredictedMostDistancePlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostDistancePlayerName, s.MostDistancePlayerName))
                    points += PtsCorrectMostDistPlayer;
            }

            // ── 21. Upset multiplier ──────────────────────────────
            bool isUpset = correctOutcome &&
                           pick.PredictedOutcome == PickOutcome.Away &&
                           match.AwayScore > match.HomeScore;

            if (isUpset)
                points = (int)Math.Round(points * UpsetMultiplier);

            // ── 22. Persist ───────────────────────────────────────
            pick.PointsAwarded = points;
            pick.IsScored = true;
            pick.IsUpset = isUpset;
            pick.ScoredAt = DateTime.UtcNow;
            pick.User.TotalPoints += points;

            totalScored++;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Scored {Count} picks.", totalScored);
        return totalScored;
    }

    private static PickOutcome GetActualOutcome(Match match)
    {
        if (match.HomeScore > match.AwayScore) return PickOutcome.Home;
        if (match.AwayScore > match.HomeScore) return PickOutcome.Away;
        return PickOutcome.Draw;
    }

    private static bool Eq(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}