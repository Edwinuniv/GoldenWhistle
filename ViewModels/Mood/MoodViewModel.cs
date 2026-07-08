namespace GoldenWhistle.ViewModels.Mood
{
    public class MoodViewModel
    {
        public int MatchId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public string MatchMinuteLabel { get; set; } = string.Empty;
        public string ScoreLabel { get; set; } = string.Empty;
        public int EcstasyPct { get; set; }
        public int AnxietyPct { get; set; }
        public int AgonyPct { get; set; }
        public int TotalVotes { get; set; }
        public int EcstasyCount { get; set; }
        public int AnxietyCount { get; set; }
        public int AgonyCount { get; set; }
        public List<MoodTimelinePoint> Timeline { get; set; } = new();
        public string? CurrentUserVote { get; set; }
    }
}
