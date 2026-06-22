namespace GoldenWhistle.Models
{
    public class League
    {
        public int Id { get; set; }
        public long ApiLeagueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
        public string? LogoUrl { get; set; }

        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}