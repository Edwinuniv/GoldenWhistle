using GoldenWhistle.Models;

namespace GoldenWhistle.Services.Interfaces
{
    public interface IFootballApiService
    {
        Task<List<Match>> FetchAndSyncMatchesAsync();
    }
}