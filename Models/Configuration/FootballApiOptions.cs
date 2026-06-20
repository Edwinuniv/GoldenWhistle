namespace GoldenWhistle.Models.Configuration
{
    public class FootballApiOptions
    {
        public const string SectionName = "FootballApi";

        public string ApiKey { get; set; } = string.Empty;
        public string ApiHost { get; set; } = string.Empty;
    }
}