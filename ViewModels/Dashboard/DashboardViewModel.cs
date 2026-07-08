using GoldenWhistle.ViewModels.Bracket;

namespace GoldenWhistle.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        public string UserDisplayName { get; set; } = string.Empty;
        public int UserTotalPoints { get; set; }
        public int UserPointsDeltaToday { get; set; }
        public int UserPredictionsMade { get; set; }
        public int UserAccuracyPct { get; set; }
        public int UserBracketRank { get; set; }
        public int TotalPlayers { get; set; }
        public List<FixtureCardViewModel> Fixtures { get; set; } = new();
        public List<BracketMatchViewModel> BracketMatches { get; set; } = new();
        public List<LeaderRowViewModel> TopLeaders { get; set; } = new();
        public List<XgDataPoint> XgByMatch { get; set; } = new();
        public int MoodEcstasyPct { get; set; }
        public int MoodAnxietyPct { get; set; }
        public int MoodAgonyPct { get; set; }
        public int MoodTotalVotes { get; set; }
    }
}
