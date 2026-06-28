namespace GoldenWhistle.Models
{
    public enum PickOutcome { Home, Away, Draw }

    public class BracketPick
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int MatchId { get; set; }

        // ── Core prediction ───────────────────────────────────────
        public PickOutcome PredictedOutcome { get; set; }
        public int? PredictedHomeScore { get; set; }
        public int? PredictedAwayScore { get; set; }

        // ── Goal scorers ──────────────────────────────────────────
        public string? PredictedFirstScorerName { get; set; }
        public string? PredictedAnytimeScorerName { get; set; }
        public string? PredictedLastScorerName { get; set; }
        public bool PredictedOwnGoal { get; set; }
        public long? PredictedOwnGoalTeamId { get; set; }

        // ── Assists ───────────────────────────────────────────────
        public string? PredictedMostAssistsPlayerName { get; set; }

        // ── Man of the Match ──────────────────────────────────────
        public string? PredictedManOfTheMatchName { get; set; }

        // ── Discipline ───────────────────────────────────────────
        public long? PredictedMostYellowsTeamId { get; set; }
        public long? PredictedMostRedsTeamId { get; set; }
        public string? PredictedMostFoulsPlayerName { get; set; }
        public long? PredictedMostFoulsTeamId { get; set; }

        // ── Set pieces ───────────────────────────────────────────
        public long? PredictedMostCornersTeamId { get; set; }

        // ── Possession & passing ─────────────────────────────────
        public long? PredictedBetterPossessionTeamId { get; set; }
        public long? PredictedMostPassesTeamId { get; set; }
        public string? PredictedMostPassesPlayerName { get; set; }

        // ── Shots & xG ───────────────────────────────────────────
        public long? PredictedHigherXgTeamId { get; set; }

        // ── Goalkeeper ───────────────────────────────────────────
        public string? PredictedMostSavesGoalkeeperName { get; set; }
        public long? PredictedMostSavesTeamId { get; set; }

        // ── Other ────────────────────────────────────────────────
        public string? PredictedMostDistancePlayerName { get; set; }

        // ── State ────────────────────────────────────────────────
        public bool IsLocked { get; set; }
        public int PointsAwarded { get; set; }
        public bool IsScored { get; set; }
        public bool IsUpset { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScoredAt { get; set; }

        // ── Navigation ───────────────────────────────────────────
        public ApplicationUser User { get; set; } = null!;
        public Match Match { get; set; } = null!;
    }
}