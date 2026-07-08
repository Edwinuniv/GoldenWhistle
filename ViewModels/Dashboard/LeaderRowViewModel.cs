namespace GoldenWhistle.ViewModels.Dashboard
{
    public class LeaderRowViewModel
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Points { get; set; }
        public int PointsDelta { get; set; }
    }
}
