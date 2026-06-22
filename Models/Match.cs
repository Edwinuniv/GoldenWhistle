namespace GoldenWhistle.Models
{
    public class Match
    {
        public int Id { get; set; }
        public long ApiMatchId { get; set; }

        public int LeagueId { get; set; }
        public League League { get; set; } = null!;

        public int HomeTeamId { get; set; }
        public Team HomeTeam { get; set; } = null!;

        public int AwayTeamId { get; set; }
        public Team AwayTeam { get; set; } = null!;

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        public DateTime KickoffUtc { get; set; }

        public bool Started { get; set; }
        public bool Finished { get; set; }
        public bool Cancelled { get; set; }
        public string StatusShort { get; set; } = string.Empty;
        public string StatusLong { get; set; } = string.Empty;

        public ICollection<MoodVote> MoodVotes { get; set; } = new List<MoodVote>();
    }
}