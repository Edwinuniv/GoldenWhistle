namespace GoldenWhistle.ViewModels.Data
{
    public class MatchStatsViewModel
    {
        public string HomeTeam { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool IsLive { get; set; }
        public int Minute { get; set; }
        public double HomeXg { get; set; }
        public double AwayXg { get; set; }
        public int HomeShots { get; set; }
        public int AwayShots { get; set; }
        public int HomePossession { get; set; }
        public int AwayPossession { get; set; }
        public int HomePasses { get; set; }
        public int AwayPasses { get; set; }
        public int HomeDuelsWon { get; set; }
        public int AwayDuelsWon { get; set; }
        public string AiSummary { get; set; } = string.Empty;
    }
}