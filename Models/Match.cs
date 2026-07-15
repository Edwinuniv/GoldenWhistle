namespace GoldenWhistle.Models
{
    // FIX (audit §3, §7): added Stage. Previously the app had no real notion
    // of tournament stage, so DashboardController.GetRound(StatusShort) and
    // BracketController's Round = League.Name were both guaranteed to fail
    // (StatusShort holds live-clock text like "36'"/"FT", League.Name holds
    // the competition name, neither is ever "QF"/"SF"/"FINAL").
    public enum MatchStage
    {
        Unknown = 0,
        RoundOf16 = 1,
        QuarterFinal = 2,
        SemiFinal = 3,
        Final = 4
    }

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

        // NEW: real tournament stage, independent from live-clock status text.
        // Defaults to Unknown until FootballApiService can derive/assign it
        // (see FootballApiService.DeriveStage — documented assumption there).
        public MatchStage Stage { get; set; } = MatchStage.Unknown;

        public ICollection<MoodVote> MoodVotes { get; set; } = new List<MoodVote>();

        // Helper used by views/controllers instead of ad-hoc string mapping.
        public string StageLabel => Stage switch
        {
            MatchStage.RoundOf16 => "R16",
            MatchStage.QuarterFinal => "QF",
            MatchStage.SemiFinal => "SF",
            MatchStage.Final => "FINAL",
            _ => "TBD"
        };
    }
}
