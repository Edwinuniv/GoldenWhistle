using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoldenWhistle.DTOs.FootballApi;

public class MatchStatsApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public MatchStatsResponseData Response { get; set; } = new();
}

public class MatchStatsResponseData
{
    [JsonPropertyName("stats")]
    public List<StatCategoryDto> Stats { get; set; } = new();
}

public class StatCategoryDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("stats")]
    public List<StatItemDto> Stats { get; set; } = new();
}

public class StatItemDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    // stats[0] = home, stats[1] = away
    // Values can be int, double, string like "460 (85%)" or null
    [JsonPropertyName("stats")]
    public List<JsonElement?> Stats { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("highlighted")]
    public string Highlighted { get; set; } = string.Empty;
}