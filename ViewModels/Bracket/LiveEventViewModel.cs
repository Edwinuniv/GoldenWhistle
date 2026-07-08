namespace GoldenWhistle.ViewModels.Bracket
{
    public class LiveEventViewModel
    {
        public int Minute { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string Score { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
