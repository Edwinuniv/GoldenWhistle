using GoldenWhistle.Data;
using GoldenWhistle.Hubs;
using GoldenWhistle.Models;
using GoldenWhistle.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GoldenWhistle.Services;

public class BracketScoringService : IBracketScoringService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BracketScoringService> _logger;
    private readonly IHubContext<LeaderboardHub>? _leaderboardHub;

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
        ILogger<BracketScoringService> logger,
        IHubContext<LeaderboardHub>? leaderboardHub = null)
    {
        _db = db;
        _logger = logger;
        _leaderboardHub = leaderboardHub;
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

            var actualOutcome = GetActualOutcome(match);
            bool correctOutcome = pick.PredictedOutcome == actualOutcome;
            if (correctOutcome)
                points += PtsCorrectOutcome;

            if (pick.PredictedHomeScore.HasValue && pick.PredictedAwayScore.HasValue)
            {
                if (pick.PredictedHomeScore == match.HomeScore &&
                    pick.PredictedAwayScore == match.AwayScore)
                    points += PtsCorrectExactScore;
            }

            if (!string.IsNullOrEmpty(pick.PredictedFirstScorerName) && s is not null)
            {
                if (Eq(pick.PredictedFirstScorerName, s.FirstScorerName))
                    points += PtsCorrectFirstScorer;
            }

            if (!string.IsNullOrEmpty(pick.PredictedLastScorerName) && s is not null)
            {
                if (Eq(pick.PredictedLastScorerName, s.LastScorerName))
                    points += PtsCorrectLastScorer;
            }

            if (!string.IsNullOrEmpty(pick.PredictedAnytimeScorerName) && s is not null)
            {
                if (s.GoalScorerNames.Any(n => Eq(n, pick.PredictedAnytimeScorerName)))
                    points += PtsCorrectAnytimeScorer;
            }

            if (pick.PredictedOwnGoal && s is not null)
            {
                if (s.OwnGoalScorerNames.Count > 0)
                    points += PtsCorrectOwnGoal;
            }

            if (!string.IsNullOrEmpty(pick.PredictedManOfTheMatchName) && s is not null)
            {
                if (Eq(pick.PredictedManOfTheMatchName, s.ManOfTheMatchName))
                    points += PtsCorrectManOfTheMatch;
            }

            if (!string.IsNullOrEmpty(pick.PredictedMostAssistsPlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostAssistsPlayerName, s.MostAssistsPlayerName))
                    points += PtsCorrectMostAssists;
            }

            if (!string.IsNullOrEmpty(pick.PredictedMostFoulsPlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostFoulsPlayerName, s.MostFoulsPlayerName))
                    points += PtsCorrectMostFoulsPlayer;
            }

            if (pick.PredictedMostFoulsTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostFoulsTeamId == s.MostFoulsTeamId)
                    points += PtsCorrectMostFoulsTeam;
            }

            if (pick.PredictedMostYellowsTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostYellowsTeamId == s.MostYellowsTeamId)
                    points += PtsCorrectYellowsTeam;
            }

            if (pick.PredictedMostRedsTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostRedsTeamId == s.MostRedsTeamId)
                    points += PtsCorrectRedsTeam;
            }

            if (pick.PredictedMostCornersTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostCornersTeamId == s.MostCornersTeamId)
                    points += PtsCorrectCornersTeam;
            }

            if (pick.PredictedBetterPossessionTeamId.HasValue && s is not null)
            {
                if (pick.PredictedBetterPossessionTeamId == s.BetterPossessionTeamId)
                    points += PtsCorrectPossessionTeam;
            }

            if (pick.PredictedMostPassesTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostPassesTeamId == s.MostPassesTeamId)
                    points += PtsCorrectPassesTeam;
            }

            if (!string.IsNullOrEmpty(pick.PredictedMostPassesPlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostPassesPlayerName, s.MostPassesPlayerName))
                    points += PtsCorrectPassesPlayer;
            }

            if (pick.PredictedHigherXgTeamId.HasValue && s is not null)
            {
                if (pick.PredictedHigherXgTeamId == s.HigherXgTeamId)
                    points += PtsCorrectXgTeam;
            }

            if (!string.IsNullOrEmpty(pick.PredictedMostSavesGoalkeeperName) && s is not null)
            {
                if (Eq(pick.PredictedMostSavesGoalkeeperName, s.MostSavesGoalkeeperName))
                    points += PtsCorrectMostSavesKeeper;
            }

            if (pick.PredictedMostSavesTeamId.HasValue && s is not null)
            {
                if (pick.PredictedMostSavesTeamId == s.MostSavesTeamId)
                    points += PtsCorrectMostSavesTeam;
            }

            if (!string.IsNullOrEmpty(pick.PredictedMostDistancePlayerName) && s is not null)
            {
                if (Eq(pick.PredictedMostDistancePlayerName, s.MostDistancePlayerName))
                    points += PtsCorrectMostDistPlayer;
            }

            // NOTE (audit §7, documented limitation — not fully fixable
            // without new data): a true "upset" requires knowing who was
            // favoured before kickoff (odds, seeding, ranking...), which
            // isn't tracked anywhere in this codebase. The previous logic
            // only checked "user picked Away and Away won", which is not an
            // upset, just a correct away pick. We keep that same narrower,
            // clearly-labelled behaviour (pick.IsUpset really means "correct
            // away-win pick") rather than inventing a fake favourite/odds
            // model. If you want real upset detection, add a
            // PreMatchFavoriteTeamId (or an odds/ranking source) to Match
            // and compare against it here.
            bool correctAwayPick = correctOutcome &&
                           pick.PredictedOutcome == PickOutcome.Away &&
                           match.AwayScore > match.HomeScore;

            if (correctAwayPick)
                points = (int)Math.Round(points * UpsetMultiplier);

            pick.PointsAwarded = points;
            pick.IsScored = true;
            pick.IsUpset = correctAwayPick;
            pick.ScoredAt = DateTime.UtcNow;
            pick.User.TotalPoints += points;

            totalScored++;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Scored {Count} picks.", totalScored);

        // NEW (audit §2/§3): push a real-time signal so Dashboard/Bracket
        // pages connected to /hubs/leaderboard actually receive
        // "LeaderboardUpdated" now that the hub exists (see Hubs/LeaderboardHub.cs).
        if (totalScored > 0 && _leaderboardHub is not null)
        {
            await LeaderboardHub.BroadcastLeaderboardUpdatedAsync(_leaderboardHub);
        }

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
