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

        // ===== ADDED: Log the raw JSON response =====
        _logger.LogInformation("Raw API Response: {Json}", json);
        // =============================================

        var apiResponse = JsonSerializer.Deserialize<LiveMatchesApiResponse>(json);

        if (apiResponse?.Response?.Live is null || apiResponse.Response.Live.Count == 0)
        {
            _logger.LogWarning("No live matches returned from API.");
            return 0;
        }

        int upsertCount = 0;

        foreach (var dto in apiResponse.Response.Live)
        {
            var league = await GetOrCreateLeagueAsync(dto.LeagueId);
            var homeTeam = await GetOrCreateTeamAsync(dto.Home.Id, dto.Home.LongName);
            var awayTeam = await GetOrCreateTeamAsync(dto.Away.Id, dto.Away.LongName);

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

            // Always update live score + status fields
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
        _logger.LogInformation("Synced {Count} live matches.", upsertCount);
        return upsertCount;
    }

    private async Task<League> GetOrCreateLeagueAsync(long apiLeagueId)
    {
        var league = await _db.Leagues.FirstOrDefaultAsync(l => l.ApiLeagueId == apiLeagueId);
        if (league is null)
        {
            league = new League
            {
                ApiLeagueId = apiLeagueId,
                Name = $"League {apiLeagueId}",  // placeholder — backfilled later
                ShortName = string.Empty,
                Country = string.Empty,
            };
            _db.Leagues.Add(league);
            await _db.SaveChangesAsync();  // flush so we have the PK
        }
        return league;
    }

    private async Task<Team> GetOrCreateTeamAsync(long apiTeamId, string name)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.ApiTeamId == apiTeamId);
        if (team is null)
        {
            team = new Team
            {
                ApiTeamId = apiTeamId,
                Name = name,
                ShortName = name.Length > 10 ? name[..10] : name,
                Country = string.Empty,
            };
            _db.Teams.Add(team);
            await _db.SaveChangesAsync();  // flush so we have the PK
        }
        return team;
    }
}