// Services/Interfaces/IFootballApiService.cs
namespace GoldenWhistle.Services.Interfaces;

public interface IFootballApiService
{
    Task<int> SyncLiveMatchesAsync();
}