namespace GoldenWhistle.ViewModels.Bracket
{
    public class LeagueStandingViewModel
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int CorrectPicks { get; set; }
        public int Points { get; set; }
    }
}
