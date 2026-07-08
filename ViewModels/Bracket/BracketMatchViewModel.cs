namespace GoldenWhistle.ViewModels.Bracket
{
    public class BracketMatchViewModel
    {
        public int MatchId { get; set; }
        public string Round { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string KickoffTime { get; set; } = string.Empty;
        public bool IsLive { get; set; }
        public bool IsWinner { get; set; }
        public string? UserPick { get; set; }
        public int PointsAwarded { get; set; }
        public bool IsScored { get; set; }
        public bool IsLocked { get; set; }
    }
}
