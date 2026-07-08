namespace GoldenWhistle.ViewModels.Shared
{
    public class SimMatchViewModel
    {
        public int MatchId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}
