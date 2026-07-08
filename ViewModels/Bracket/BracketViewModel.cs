namespace GoldenWhistle.ViewModels.Bracket
{
    public class BracketViewModel
    {
        public int TotalCorrect { get; set; }
        public int TotalPending { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public int MatchId { get; set; }
        public string? UserPick { get; set; }
        public int PointsAwarded { get; set; }
        public bool IsScored { get; set; }
        public bool IsLocked { get; set; }
        public List<BracketMatchViewModel> Picks { get; set; } = new();
        public List<LeagueStandingViewModel> LeagueStandings { get; set; } = new();
        public List<LiveEventViewModel> LiveEvents { get; set; } = new();
    }
}
