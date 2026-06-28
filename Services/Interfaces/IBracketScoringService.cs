namespace GoldenWhistle.Services.Interfaces;

public interface IBracketScoringService
{
    /// <summary>
    /// Scores all finished, unscored matches. Called after every sync.
    /// Returns the number of picks scored.
    /// </summary>
    Task<int> ScoreFinishedMatchesAsync();
}