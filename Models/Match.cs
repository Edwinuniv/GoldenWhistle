namespace GoldenWhistle.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int ApiFootballId { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string HomeTeamLogo { get; set; } = string.Empty;
        public string AwayTeamLogo { get; set; } = string.Empty;
        public DateTime KickoffUtc { get; set; }
        public string Status { get; set; } = "NS"; // NS=Not Started, LIVE, FT
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string League { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;

        public ICollection<MoodVote> MoodVotes { get; set; } = new List<MoodVote>();
    }
}