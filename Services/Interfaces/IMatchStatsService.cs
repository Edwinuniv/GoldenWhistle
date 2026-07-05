namespace GoldenWhistle.Services.Interfaces;

public interface IMatchStatsService
{
    /// <summary>
    /// Fetches and stores stats for all finished matches
    /// that don't have complete stats yet.
    /// Returns the number of matches processed.
    /// </summary>
    Task<int> SyncMatchStatsAsync();

    /// <summary>
    /// Fetches and stores stats for a single match by its DB id.
    /// </summary>
    Task<bool> SyncSingleMatchStatsAsync(int matchId);
}