using System.Net.Http.Headers;
using System.Text.Json;
using GoldenWhistle.Data;
using GoldenWhistle.DTOs.FootballApi;
using GoldenWhistle.Models;
using GoldenWhistle.Models.Configuration;
using GoldenWhistle.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoldenWhistle.Services
{
    public class FootballApiService : IFootballApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _db;
        private readonly FootballApiOptions _options;
        private readonly ILogger<FootballApiService> _logger;

        private const string MatchesEndpoint = "/football-current-list";

        public FootballApiService(
            HttpClient httpClient,
            ApplicationDbContext db,
            IOptions<FootballApiOptions> options,
            ILogger<FootballApiService> logger)
        {
            _httpClient = httpClient;
            _db = db;
            _options = options.Value;
            _logger = logger;

            _httpClient.BaseAddress = new Uri($"https://{_options.ApiHost}");
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", _options.ApiHost);
        }

        public async Task<List<Match>> FetchAndSyncMatchesAsync()
        {
            var response = await _httpClient.GetAsync(MatchesEndpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<MatchesApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.Response?.Matches == null)
            {
                _logger.LogWarning("Football API returned no matches.");
                return new List<Match>();
            }

            var syncedMatches = new List<Match>();

            foreach (var dto in apiResponse.Response.Matches)
            {
                var league = await GetOrCreateLeagueAsync(dto.LeagueId);
                var homeTeam = await GetOrCreateTeamAsync(dto.Home.Id, dto.Home.Name, dto.Home.LongName, league.Id);
                var awayTeam = await GetOrCreateTeamAsync(dto.Away.Id, dto.Away.Name, dto.Away.LongName, league.Id);

                var match = await _db.Matches.FirstOrDefaultAsync(m => m.ApiMatchId == dto.Id);

                if (match == null)
                {
                    match = new Match { ApiMatchId = dto.Id };
                    _db.Matches.Add(match);
                }

                match.LeagueId = league.Id;
                match.HomeTeamId = homeTeam.Id;
                match.AwayTeamId = awayTeam.Id;
                match.HomeScore = dto.Home.Score;
                match.AwayScore = dto.Away.Score;
                match.KickoffUtc = dto.Status.UtcTime;
                match.Started = dto.Status.Started;
                match.Finished = dto.Status.Finished;
                match.Cancelled = dto.Status.Cancelled;
                match.StatusShort = dto.Status.Reason?.Short ?? string.Empty;
                match.StatusLong = dto.Status.Reason?.Long ?? string.Empty;

                syncedMatches.Add(match);
            }

            await _db.SaveChangesAsync();
            return syncedMatches;
        }

        private async Task<League> GetOrCreateLeagueAsync(int apiLeagueId)
        {
            var league = await _db.Leagues.FirstOrDefaultAsync(l => l.ApiLeagueId == apiLeagueId);
            if (league != null) return league;

            league = new League
            {
                ApiLeagueId = apiLeagueId,
                Name = $"League {apiLeagueId}",
                ShortName = $"League {apiLeagueId}",
                Country = string.Empty
            };

            _db.Leagues.Add(league);
            await _db.SaveChangesAsync();
            return league;
        }

        private async Task<Team> GetOrCreateTeamAsync(int apiTeamId, string name, string longName, int leagueId)
        {
            var team = await _db.Teams.FirstOrDefaultAsync(t => t.ApiTeamId == apiTeamId);
            if (team != null) return team;

            team = new Team
            {
                ApiTeamId = apiTeamId,
                Name = name,
                ShortName = longName,
                Country = string.Empty,
                PrimaryLeagueId = leagueId
            };

            _db.Teams.Add(team);
            await _db.SaveChangesAsync();
            return team;
        }
    }
}