using GoldenWhistle.Services.Interfaces;

namespace GoldenWhistle.BackgroundServices
{
    public class SyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SyncBackgroundService> _logger;

        // Poll every 2 minutes during expected match hours,
        // every 30 minutes otherwise
        private static readonly TimeSpan ActiveInterval = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan IdleInterval = TimeSpan.FromMinutes(30);

        public SyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunSyncAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync failed.");
                }

                var delay = IsActiveHour() ? ActiveInterval : IdleInterval;
                _logger.LogInformation("Next sync in {Delay}.", delay);
                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task RunSyncAsync()
        {
            // BackgroundService is singleton — services must be resolved
            // in a scope because DbContext is scoped
            using var scope = _scopeFactory.CreateScope();

            var footballService = scope.ServiceProvider
                .GetRequiredService<IFootballApiService>();
            var scoringService = scope.ServiceProvider
                .GetRequiredService<IBracketScoringService>();
            var statsService = scope.ServiceProvider
                .GetRequiredService<IMatchStatsService>();  // <-- NEW

            var matchCount = await footballService.SyncLiveMatchesAsync();
            var scoredCount = await scoringService.ScoreFinishedMatchesAsync();
            var statsCount = await statsService.SyncMatchStatsAsync();  // <-- NEW

            _logger.LogInformation(
                "Background sync — {Matches} matches, {Scored} picks scored, {Stats} stats synced.",
                matchCount, scoredCount, statsCount);  // <-- UPDATED
        }

        private static bool IsActiveHour()
        {
            var hour = DateTime.UtcNow.Hour;
            return hour >= 12 && hour <= 23;
        }
    }
}