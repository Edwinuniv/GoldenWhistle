namespace GoldenWhistle.Models
{
    public class Team
    {
        public int Id { get; set; }
        public long ApiTeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        public int? PrimaryLeagueId { get; set; }
        public League? PrimaryLeague { get; set; }
    }
}