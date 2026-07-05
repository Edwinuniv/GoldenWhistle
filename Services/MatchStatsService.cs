using System.Text.Json;
using GoldenWhistle.Data;
using GoldenWhistle.DTOs.FootballApi;
using GoldenWhistle.Models;
using GoldenWhistle.Models.Configuration;
using GoldenWhistle.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoldenWhistle.Services;

public class MatchStatsService : IMatchStatsService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MatchStatsService> _logger;

    private const string StatsEndpoint = "/football-get-match-stats";

    public MatchStatsService(
        HttpClient httpClient,
        ApplicationDbContext db,
        IOptions<FootballApiOptions> options,
        ILogger<MatchStatsService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;

        var opts = options.Value;
        _httpClient.BaseAddress = new Uri("https://free-api-live-football-data.p.rapidapi.com");
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", opts.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", opts.ApiHost);
    }

    public async Task<int> SyncMatchStatsAsync()
    {
        // Only process finished matches without complete stats
        var matches = await _db.Matches
            .Where(m => m.Finished)
            .ToListAsync();

        var completedStatMatchIds = await _db.MatchStats
            .Where(s => s.IsComplete)
            .Select(s => s.MatchId)
            .ToListAsync();

        var toProcess = matches
            .Where(m => !completedStatMatchIds.Contains(m.Id))
            .ToList();

        if (toProcess.Count == 0)
        {
            _logger.LogInformation("All finished matches already have complete stats.");
            return 0;
        }

        int processed = 0;
        foreach (var match in toProcess)
        {
            try
            {
                var success = await FetchAndSaveStatsAsync(match);
                if (success) processed++;

                // Respect API rate limits — small delay between calls
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch stats for match {MatchId}.", match.Id);
            }
        }

        _logger.LogInformation("Synced stats for {Count} matches.", processed);
        return processed;
    }

    public async Task<bool> SyncSingleMatchStatsAsync(int matchId)
    {
        var match = await _db.Matches.FindAsync(matchId);
        if (match is null) return false;
        return await FetchAndSaveStatsAsync(match);
    }

    private async Task<bool> FetchAndSaveStatsAsync(Match match)
    {
        var url = $"{StatsEndpoint}?eventid={match.ApiMatchId}";
        var json = await _httpClient.GetStringAsync(url);

        var apiResponse = JsonSerializer.Deserialize<MatchStatsApiResponse>(json);
        if (apiResponse?.Response?.Stats is null || apiResponse.Response.Stats.Count == 0)
        {
            _logger.LogWarning("No stats returned for match {ApiMatchId}.", match.ApiMatchId);
            return false;
        }

        // Build a lookup: key → StatItemDto for fast access
        var lookup = apiResponse.Response.Stats
            .SelectMany(cat => cat.Stats)
            .Where(s => s.Type != "title")
            .GroupBy(s => s.Key)
            .ToDictionary(g => g.Key, g => g.First());

        // Find or create the MatchStats row
        var stats = await _db.MatchStats
            .FirstOrDefaultAsync(s => s.MatchId == match.Id);

        if (stats is null)
        {
            stats = new MatchStats { MatchId = match.Id };
            _db.MatchStats.Add(stats);
        }

        // ── Possession ───────────────────────────────────────────
        if (lookup.TryGetValue("BallPossesion", out var poss))
        {
            stats.HomePossessionPct = GetDouble(poss, 0);
            stats.AwayPossessionPct = GetDouble(poss, 1);
            stats.BetterPossessionTeamId = stats.HomePossessionPct >= stats.AwayPossessionPct
                ? match.HomeTeamId : match.AwayTeamId;
        }

        // ── xG ───────────────────────────────────────────────────
        if (lookup.TryGetValue("expected_goals", out var xg))
        {
            stats.HomeXg = GetDouble(xg, 0);
            stats.AwayXg = GetDouble(xg, 1);
            stats.HigherXgTeamId = stats.HomeXg >= stats.AwayXg
                ? match.HomeTeamId : match.AwayTeamId;
        }

        // ── Shots ─────────────────────────────────────────────────
        if (lookup.TryGetValue("total_shots", out var shots))
        {
            stats.HomeShotsTotal = GetInt(shots, 0);
            stats.AwayShotsTotal = GetInt(shots, 1);
        }

        if (lookup.TryGetValue("ShotsOnTarget", out var onTarget))
        {
            stats.HomeShotsOnTarget = GetInt(onTarget, 0);
            stats.AwayShotsOnTarget = GetInt(onTarget, 1);
        }

        // ── Passes ───────────────────────────────────────────────
        if (lookup.TryGetValue("passes", out var passes))
        {
            stats.HomePasses = GetInt(passes, 0);
            stats.AwayPasses = GetInt(passes, 1);
            stats.MostPassesTeamId = stats.HomePasses >= stats.AwayPasses
                ? match.HomeTeamId : match.AwayTeamId;
        }

        if (lookup.TryGetValue("accurate_passes", out var accPasses))
        {
            // Format is "460 (85%)" — extract the percentage
            stats.HomePassAccuracyPct = ParsePercentage(accPasses, 0);
            stats.AwayPassAccuracyPct = ParsePercentage(accPasses, 1);
        }

        // ── Corners ───────────────────────────────────────────────
        if (lookup.TryGetValue("corners", out var corners))
        {
            stats.HomeCorners = GetInt(corners, 0);
            stats.AwayCorners = GetInt(corners, 1);
            stats.MostCornersTeamId = stats.HomeCorners >= stats.AwayCorners
                ? match.HomeTeamId : match.AwayTeamId;
        }

        // ── Discipline ───────────────────────────────────────────
        if (lookup.TryGetValue("yellow_cards", out var yellows))
        {
            stats.HomeYellowCards = GetInt(yellows, 0);
            stats.AwayYellowCards = GetInt(yellows, 1);
            stats.MostYellowsTeamId = stats.HomeYellowCards >= stats.AwayYellowCards
                ? match.HomeTeamId : match.AwayTeamId;
        }

        if (lookup.TryGetValue("red_cards", out var reds))
        {
            stats.HomeRedCards = GetInt(reds, 0);
            stats.AwayRedCards = GetInt(reds, 1);
            stats.MostRedsTeamId = stats.HomeRedCards >= stats.AwayRedCards
                ? match.HomeTeamId : match.AwayTeamId;
        }

        if (lookup.TryGetValue("fouls", out var fouls))
        {
            stats.HomeFouls = GetInt(fouls, 0);
            stats.AwayFouls = GetInt(fouls, 1);
            stats.MostFoulsTeamId = stats.HomeFouls >= stats.AwayFouls
                ? match.HomeTeamId : match.AwayTeamId;
        }

        // ── Saves ────────────────────────────────────────────────
        if (lookup.TryGetValue("keeper_saves", out var saves))
        {
            stats.HomeSaves = GetInt(saves, 0);
            stats.AwaySaves = GetInt(saves, 1);
            stats.MostSavesTeamId = stats.HomeSaves >= stats.AwaySaves
                ? match.HomeTeamId : match.AwayTeamId;
        }

        // ── Duels ────────────────────────────────────────────────
        if (lookup.TryGetValue("duel_won", out var duels))
        {
            stats.HomeDuelsWon = GetInt(duels, 0);
            stats.AwayDuelsWon = GetInt(duels, 1);
        }

        if (lookup.TryGetValue("aerials_won", out var aerials))
        {
            stats.HomeAerialDuelsWon = ParseIntFromPercentageString(aerials, 0);
            stats.AwayAerialDuelsWon = ParseIntFromPercentageString(aerials, 1);
        }

        // ── Tackles & interceptions ───────────────────────────────
        if (lookup.TryGetValue("matchstats.headers.tackles", out var tackles))
        {
            stats.HomeTackles = GetInt(tackles, 0);
            stats.AwayTackles = GetInt(tackles, 1);
        }

        if (lookup.TryGetValue("interceptions", out var interceptions))
        {
            stats.HomeInterceptions = GetInt(interceptions, 0);
            stats.AwayInterceptions = GetInt(interceptions, 1);
        }

        // ── Offsides ─────────────────────────────────────────────
        if (lookup.TryGetValue("Offsides", out var offsides))
        {
            stats.HomeOffsides = GetInt(offsides, 0);
            stats.AwayOffsides = GetInt(offsides, 1);
        }

        // ── Distance covered ─────────────────────────────────────
        if (lookup.TryGetValue("physical_metrics_distance_covered", out var dist))
        {
            // API returns in metres — convert to km
            stats.HomeDistanceCoveredKm = GetDouble(dist, 0) / 1000.0;
            stats.AwayDistanceCoveredKm = GetDouble(dist, 1) / 1000.0;
        }

        // ── Meta ─────────────────────────────────────────────────
        stats.FetchedAt = DateTime.UtcNow;
        stats.IsComplete = true;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Stats saved for match {ApiMatchId}.", match.ApiMatchId);
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static int? GetInt(StatItemDto stat, int index)
    {
        if (stat.Stats.Count <= index || stat.Stats[index] is null) return null;
        var el = stat.Stats[index]!.Value;
        return el.ValueKind == JsonValueKind.Number ? el.GetInt32() : null;
    }

    private static double? GetDouble(StatItemDto stat, int index)
    {
        if (stat.Stats.Count <= index || stat.Stats[index] is null) return null;
        var el = stat.Stats[index]!.Value;
        if (el.ValueKind == JsonValueKind.Number) return el.GetDouble();
        if (el.ValueKind == JsonValueKind.String &&
            double.TryParse(el.GetString(), out var d)) return d;
        return null;
    }

    // Parses "460 (85%)" → 85.0
    private static double? ParsePercentage(StatItemDto stat, int index)
    {
        if (stat.Stats.Count <= index || stat.Stats[index] is null) return null;
        var el = stat.Stats[index]!.Value;
        if (el.ValueKind != JsonValueKind.String) return null;
        var raw = el.GetString() ?? "";
        var start = raw.IndexOf('(');
        var end = raw.IndexOf('%');
        if (start < 0 || end < 0) return null;
        var numStr = raw.Substring(start + 1, end - start - 1).Trim();
        return double.TryParse(numStr, out var pct) ? pct : null;
    }

    // Parses "19 (63%)" → 19
    private static int? ParseIntFromPercentageString(StatItemDto stat, int index)
    {
        if (stat.Stats.Count <= index || stat.Stats[index] is null) return null;
        var el = stat.Stats[index]!.Value;
        if (el.ValueKind != JsonValueKind.String) return null;
        var raw = el.GetString() ?? "";
        var part = raw.Split('(')[0].Trim();
        return int.TryParse(part, out var n) ? n : null;
    }
}