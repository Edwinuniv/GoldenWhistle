// DTOs/FootballApi/LiveMatchesResponseDto.cs
using System.Text.Json.Serialization;

namespace GoldenWhistle.DTOs.FootballApi;

public class LiveMatchesApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public LiveMatchesResponseData Response { get; set; } = new();
}

public class LiveMatchesResponseData
{
    [JsonPropertyName("live")]
    public List<LiveMatchDto> Live { get; set; } = new();
}

public class LiveMatchDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("leagueId")]
    public long LeagueId { get; set; }

    [JsonPropertyName("home")]
    public TeamSideDto Home { get; set; } = new();

    [JsonPropertyName("away")]
    public TeamSideDto Away { get; set; } = new();

    [JsonPropertyName("status")]
    public LiveMatchStatusDto Status { get; set; } = new();

    [JsonPropertyName("timeTS")]
    public long TimeTS { get; set; }
}

public class TeamSideDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("longName")]
    public string LongName { get; set; } = string.Empty;
}

public class LiveMatchStatusDto
{
    [JsonPropertyName("utcTime")]
    public string UtcTime { get; set; } = string.Empty;

    [JsonPropertyName("started")]
    public bool Started { get; set; }

    [JsonPropertyName("finished")]
    public bool Finished { get; set; }

    [JsonPropertyName("cancelled")]
    public bool Cancelled { get; set; }

    [JsonPropertyName("ongoing")]
    public bool Ongoing { get; set; }

    [JsonPropertyName("scoreStr")]
    public string? ScoreStr { get; set; }

    [JsonPropertyName("liveTime")]
    public LiveTimeDto? LiveTime { get; set; }
}

public class LiveTimeDto
{
    [JsonPropertyName("short")]
    public string Short { get; set; } = string.Empty;  // e.g. "36'" or "HT"

    [JsonPropertyName("long")]
    public string Long { get; set; } = string.Empty;   // e.g. "35:30"
}