namespace GoldenWhistle.ViewModels.Kickoff
{
    public class KickoffMatchViewModel
    {
        public int MatchId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public DateTime KickoffUtc { get; set; }
        public string StadiumInfo { get; set; } = string.Empty;
        public List<InjuryItemViewModel> HomeInjuries { get; set; } = new();
        public List<InjuryItemViewModel> AwayInjuries { get; set; } = new();
        public TacticViewModel? HomeTactic { get; set; }
        public TacticViewModel? AwayTactic { get; set; }
        public List<FactViewModel> Facts { get; set; } = new();
        public H2HViewModel? H2H { get; set; }
    }
}
