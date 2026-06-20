using System.Text.Json.Serialization;

namespace GoldenWhistle.DTOs.FootballApi
{
    public class MatchesApiResponse
    {
        [JsonPropertyName("response")]
        public MatchesResponseData Response { get; set; } = new();
    }

    public class MatchesResponseData
    {
        [JsonPropertyName("matches")]
        public List<MatchDto> Matches { get; set; } = new();
    }

    public class MatchDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("leagueId")]
        public int LeagueId { get; set; }

        [JsonPropertyName("home")]
        public TeamSideDto Home { get; set; } = new();

        [JsonPropertyName("away")]
        public TeamSideDto Away { get; set; } = new();

        [JsonPropertyName("status")]
        public MatchStatusDto Status { get; set; } = new();
    }

    public class TeamSideDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("longName")]
        public string LongName { get; set; } = string.Empty;
    }

    public class MatchStatusDto
    {
        [JsonPropertyName("utcTime")]
        public DateTime UtcTime { get; set; }

        [JsonPropertyName("started")]
        public bool Started { get; set; }

        [JsonPropertyName("finished")]
        public bool Finished { get; set; }

        [JsonPropertyName("cancelled")]
        public bool Cancelled { get; set; }

        [JsonPropertyName("reason")]
        public MatchStatusReasonDto? Reason { get; set; }
    }

    public class MatchStatusReasonDto
    {
        [JsonPropertyName("short")]
        public string Short { get; set; } = string.Empty;

        [JsonPropertyName("long")]
        public string Long { get; set; } = string.Empty;
    }
}