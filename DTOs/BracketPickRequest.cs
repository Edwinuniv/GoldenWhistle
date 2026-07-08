namespace GoldenWhistle.DTOs
{
    public class BracketPickRequest
    {
        public int MatchId { get; set; }
        public string PredictedOutcome { get; set; } = string.Empty;
        public int? PredictedHomeScore { get; set; }
        public int? PredictedAwayScore { get; set; }

        public string? PredictedFirstScorerName { get; set; }
        public string? PredictedLastScorerName { get; set; }
        public string? PredictedAnytimeScorerName { get; set; }
        public bool PredictedOwnGoal { get; set; }
        public long? PredictedOwnGoalTeamId { get; set; }

        public string? PredictedMostAssistsPlayerName { get; set; }
        public string? PredictedManOfTheMatchName { get; set; }

        public long? PredictedMostYellowsTeamId { get; set; }
        public long? PredictedMostRedsTeamId { get; set; }
        public long? PredictedMostFoulsTeamId { get; set; }
        public string? PredictedMostFoulsPlayerName { get; set; }

        public long? PredictedMostCornersTeamId { get; set; }

        public long? PredictedBetterPossessionTeamId { get; set; }
        public long? PredictedMostPassesTeamId { get; set; }
        public string? PredictedMostPassesPlayerName { get; set; }

        public long? PredictedHigherXgTeamId { get; set; }

        public string? PredictedMostSavesGoalkeeperName { get; set; }
        public long? PredictedMostSavesTeamId { get; set; }

        public string? PredictedMostDistancePlayerName { get; set; }
    }
}