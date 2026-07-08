namespace GoldenWhistle.ViewModels.Dashboard
{
    public class FixtureCardViewModel
    {
        public int MatchId { get; set; }
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string StatusBadge { get; set; } = string.Empty;
        public string KickoffTime { get; set; } = string.Empty;
        public string MatchDate { get; set; } = string.Empty;
        public bool IsLive { get; set; }
    }
}
