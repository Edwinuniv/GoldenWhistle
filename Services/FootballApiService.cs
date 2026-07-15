// Services/FootballApiService.cs
using System.Text.Json;
using GoldenWhistle.Data;
using GoldenWhistle.DTOs.FootballApi;
using GoldenWhistle.Models;
using GoldenWhistle.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GoldenWhistle.Models.Configuration;

namespace GoldenWhistle.Services;

public class FootballApiService : IFootballApiService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FootballApiService> _logger;

    private const string LiveMatchesEndpoint = "/football-current-live";

    public FootballApiService(
        HttpClient httpClient,
        ApplicationDbContext db,
        IOptions<FootballApiOptions> options,
        ILogger<FootballApiService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;

        var opts = options.Value;
        _httpClient.BaseAddress = new Uri("https://free-api-live-football-data.p.rapidapi.com");
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", opts.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", opts.ApiHost);
    }

    public async Task<int> SyncLiveMatchesAsync()
    {
        _logger.LogInformation("Fetching live matches from API...");

        var json = await _httpClient.GetStringAsync(LiveMatchesEndpoint);
        var apiResponse = JsonSerializer.Deserialize<LiveMatchesApiResponse>(json);

        if (apiResponse?.Response?.Live is null || apiResponse.Response.Live.Count == 0)
        {
            _logger.LogWarning("No live matches returned from API.");
            await MarkStaleMatchesAsFinishedAsync();
            return 0;
        }

        int upsertCount = 0;

        foreach (var dto in apiResponse.Response.Live)
        {
            var league = await GetOrCreateLeagueAsync(dto.LeagueId);
            var homeTeam = await GetOrCreateTeamAsync(dto.Home.Id, dto.Home.Name, dto.Home.LongName);
            var awayTeam = await GetOrCreateTeamAsync(dto.Away.Id, dto.Away.Name, dto.Away.LongName);

            var kickoff = DateTimeOffset.TryParse(dto.Status.UtcTime, out var parsed)
                ? parsed.UtcDateTime
                : DateTimeOffset.FromUnixTimeMilliseconds(dto.TimeTS).UtcDateTime;

            var match = await _db.Matches.FirstOrDefaultAsync(m => m.ApiMatchId == dto.Id);

            if (match is null)
            {
                match = new Match
                {
                    ApiMatchId = dto.Id,
                    LeagueId = league.Id,
                    HomeTeamId = homeTeam.Id,
                    AwayTeamId = awayTeam.Id,
                    KickoffUtc = kickoff,
                };
                _db.Matches.Add(match);
            }

            match.HomeScore = dto.Home.Score;
            match.AwayScore = dto.Away.Score;
            match.Started = dto.Status.Started;
            match.Finished = dto.Status.Finished;
            match.Cancelled = dto.Status.Cancelled;
            match.StatusShort = dto.Status.LiveTime?.Short ?? dto.Status.ScoreStr ?? string.Empty;
            match.StatusLong = dto.Status.LiveTime?.Long ?? string.Empty;

            upsertCount++;
        }

        await _db.SaveChangesAsync();

        // FIX (audit §3/§7): the free-tier API does not expose a "round"
        // field on the live-match DTO, so there is no ground truth for
        // stage. As a documented, best-effort approximation, we derive
        // stage per-league from chronological order assuming a standard
        // single-elimination bracket (8 QF -> 4 SF -> 2 Final, from a
        // Round-of-16 stage of 16 matches if present). This only produces
        // correct labels once all matches for the tournament have been
        // synced at least once. If the paid/full API tier exposes an
        // explicit round field, replace this with that field directly.
        await DeriveStagesAsync();

        await MarkStaleMatchesAsFinishedAsync();

        _logger.LogInformation("Synced {Count} live matches.", upsertCount);
        return upsertCount;
    }

    private async Task DeriveStagesAsync()
    {
        var leagueIds = await _db.Matches.Select(m => m.LeagueId).Distinct().ToListAsync();

        foreach (var leagueId in leagueIds)
        {
            var matches = await _db.Matches
                .Where(m => m.LeagueId == leagueId && !m.Cancelled)
                .OrderBy(m => m.KickoffUtc)
                .ToListAsync();

            int count = matches.Count;
            if (count == 0) continue;

            // Standard bracket sizes counting backwards from the final.
            // Anything beyond Round of 16 in this same league is left as
            // Unknown rather than guessed, since group-stage/earlier knockout
            // rounds are not modelled by this app yet.
            for (int i = 0; i < count; i++)
            {
                int fromEnd = count - i; // 1 = last match chronologically
                matches[i].Stage = fromEnd switch
                {
                    1 or 2 => MatchStage.Final,
                    <= 4 => MatchStage.SemiFinal,
                    <= 8 => MatchStage.QuarterFinal,
                    <= 16 => MatchStage.RoundOf16,
                    _ => MatchStage.Unknown
                };
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task MarkStaleMatchesAsFinishedAsync()
    {
        // NOTE (audit §7): still a heuristic (kickoff + 2h => Finished)
        // because the DTO doesn't reliably give us a definitive "match over"
        // flag in all cases seen so far. If a definitive full-time flag is
        // available in your API tier, prefer that over this timeout.
        var cutoff = DateTime.UtcNow.AddHours(-2);

        var staleMatches = await _db.Matches
            .Where(m => m.Started && !m.Finished && m.KickoffUtc < cutoff)
            .ToListAsync();

        if (staleMatches.Count == 0) return;

        foreach (var match in staleMatches)
        {
            match.Finished = true;
            match.StatusShort = "FT";
            match.StatusLong = "Full-Time";
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Marked {Count} stale matches as finished.", staleMatches.Count);
    }

    private async Task<League> GetOrCreateLeagueAsync(long apiLeagueId)
    {
        var league = await _db.Leagues.FirstOrDefaultAsync(l => l.ApiLeagueId == apiLeagueId);
        if (league is null)
        {
            league = new League
            {
                ApiLeagueId = apiLeagueId,
                Name = $"League {apiLeagueId}",  // placeholder — see README follow-up: no backfill service exists yet
                ShortName = string.Empty,
                Country = string.Empty,
            };
            _db.Leagues.Add(league);
            await _db.SaveChangesAsync();
        }
        return league;
    }

    // FIX (audit §7): ShortName used to be a blind substring of the full
    // name (name[..10]), producing meaningless abbreviations. We now prefer
    // the API's own short "name" field (e.g. "BRA") when present, falling
    // back to a safe truncation only if it's missing.
    private async Task<Team> GetOrCreateTeamAsync(long apiTeamId, string shortNameFromApi, string longName)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.ApiTeamId == apiTeamId);
        if (team is null)
        {
            team = new Team
            {
                ApiTeamId = apiTeamId,
                Name = string.IsNullOrWhiteSpace(longName) ? shortNameFromApi : longName,
                ShortName = !string.IsNullOrWhiteSpace(shortNameFromApi)
                    ? shortNameFromApi.ToUpperInvariant()
                    : (longName.Length > 3 ? longName[..3].ToUpperInvariant() : longName.ToUpperInvariant()),
                Country = string.Empty,
            };
            _db.Teams.Add(team);
            await _db.SaveChangesAsync();
        }
        return team;
    }
}
