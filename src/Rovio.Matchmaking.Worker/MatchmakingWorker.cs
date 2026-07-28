using Microsoft.Extensions.Options;
using Rovio.Matchmaking.Application.Abstractions;
using Rovio.Matchmaking.Application.Options;

namespace Rovio.Matchmaking.Worker;

public sealed class MatchmakingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MatchmakingOptions> options,
    ILogger<MatchmakingWorker> logger) : BackgroundService
{
    private readonly MatchmakingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Matchmaking worker started (interval {IntervalMs}ms)",
            _options.WorkerIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IMatchmakingEngine>();
                await engine.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Matchmaking worker iteration failed");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(_options.WorkerIntervalMs),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
