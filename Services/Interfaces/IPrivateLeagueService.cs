using GoldenWhistle.Models;
namespace GoldenWhistle.Services.Interfaces;

public interface IPrivateLeagueService
{
    Task<PrivateLeague> CreateLeagueAsync(string userId, string leagueName);
    Task<PrivateLeague?> JoinLeagueAsync(string userId, string joinCode);
    Task<List<LeagueMember>> GetLeaderboardAsync(int leagueId);
}