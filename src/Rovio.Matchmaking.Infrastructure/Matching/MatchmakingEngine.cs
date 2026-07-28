namespace Rovio.Matchmaking.Infrastructure.Matching;

public sealed class MatchmakingEngine(
    IGameConfigRuntime configRuntime,
    ITicketStore ticketStore,
    ISessionStore sessionStore,
    IShardLock shardLock,
    TimeProvider timeProvider,
    IConnectionMultiplexer redis,
    IOptions<MatchmakingOptions> options,
    ILogger<MatchmakingEngine> logger) : IMatchmakingEngine
{
    private readonly MatchingService _matchingService = new();
    private readonly MatchmakingOptions _options = options.Value;

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var gameIds = await configRuntime.ListGameIdsAsync(cancellationToken);
        foreach (var gameId in gameIds)
        {
            var config = await configRuntime.GetAsync(gameId, cancellationToken);
            if (config is null || !config.Enabled)
            {
                continue;
            }

            var regions = await DiscoverRegionsAsync(gameId, cancellationToken);
            foreach (var region in regions)
            {
                await using var lease = await shardLock.TryAcquireAsync(
                    gameId,
                    region,
                    TimeSpan.FromSeconds(_options.ShardLockSeconds),
                    cancellationToken);

                if (lease is null)
                {
                    continue;
                }

                try
                {
                    await MatchRegionAsync(config, region, cancellationToken);
                    await LateJoinPassAsync(config, region, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Match pass failed for {GameId}/{Region}", gameId, region);
                }
            }
        }
    }

    private async Task MatchRegionAsync(GameMatchConfig config, MatchRegion region, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var candidatesResult = await ticketStore.GetQueuedCandidatesAsync(
            config.GameId,
            region,
            _options.MatchCandidateLimit,
            cancellationToken);

        if (candidatesResult.IsFailure)
        {
            logger.LogWarning("Failed to load candidates for {GameId}/{Region}: {Error}",
                config.GameId, region, candidatesResult.Error.ErrorMessage);
            return;
        }

        var candidates = candidatesResult.Value.ToList();
        while (candidates.Count >= config.PlayerCapacity.MinPlayerCount)
        {
            var groupResult = _matchingService.SelectMatchGroup(candidates, config, region, now);
            if (groupResult.IsFailure)
            {
                logger.LogWarning(
                    "Matching failed for {GameId}/{Region}: {Error}",
                    config.GameId,
                    region,
                    groupResult.Error.ErrorMessage);
                break;
            }

            var group = groupResult.Value;
            if (group.Count == 0)
            {
                break;
            }

            var sessionResult = GameSession.Create(
                config.GameId,
                region,
                config.PlayerCapacity,
                config.AllowLateJoin,
                group.Select(t => t.PlayerId).ToList(),
                now);

            if (sessionResult.IsFailure)
            {
                break;
            }

            var form = await sessionStore.FormSessionAsync(
                sessionResult.Value,
                group.Select(t => t.Id).ToList(),
                cancellationToken);

            if (form.IsFailure)
            {
                logger.LogDebug("FormSession race for {GameId}/{Region}: {Error}",
                    config.GameId, region, form.Error.ErrorMessage);
                break;
            }

            var claimed = group.Select(t => t.Id.Value).ToHashSet();
            candidates = candidates.Where(t => !claimed.Contains(t.Id.Value)).ToList();
        }
    }

    private async Task LateJoinPassAsync(GameMatchConfig config, MatchRegion region, CancellationToken cancellationToken)
    {
        if (!config.AllowLateJoin)
        {
            return;
        }

        var openResult = await sessionStore.GetOpenSessionsAsync(config.GameId, region, cancellationToken);
        if (openResult.IsFailure || openResult.Value.Count == 0)
        {
            return;
        }

        var openSessions = openResult.Value.ToList();
        var candidatesResult = await ticketStore.GetQueuedCandidatesAsync(
            config.GameId,
            region,
            _options.MatchCandidateLimit,
            cancellationToken);

        if (candidatesResult.IsFailure)
        {
            return;
        }

        foreach (var ticket in candidatesResult.Value.OrderBy(t => t.EnqueuedAt))
        {
            var session = openSessions.FirstOrDefault(s => s.CanLateJoin);
            if (session is null)
            {
                break;
            }

            var lateJoin = await sessionStore.LateJoinAsync(
                session.Id,
                config.GameId,
                region,
                ticket.Id,
                ticket.PlayerId,
                cancellationToken);

            if (lateJoin.IsFailure)
            {
                logger.LogDebug("Late join race for ticket {TicketId}: {Error}",
                    ticket.Id, lateJoin.Error.ErrorMessage);
                continue;
            }

            var refreshed = await sessionStore.GetAsync(session.Id, cancellationToken);
            if (refreshed.IsFailure || !refreshed.Value.CanLateJoin)
            {
                openSessions = openSessions.Where(s => s.Id != session.Id).ToList();
            }
            else
            {
                openSessions = openSessions
                    .Select(s => s.Id.Equals(refreshed.Value.Id) ? refreshed.Value : s)
                    .ToList();
            }
        }
    }

    private async Task<IReadOnlyList<MatchRegion>> DiscoverRegionsAsync(GameId gameId, CancellationToken cancellationToken)
    {
        var regions = new HashSet<string>(_options.SeedRegions, StringComparer.OrdinalIgnoreCase);
        var server = redis.GetServers().FirstOrDefault(s => s.IsConnected);
        if (server is not null)
        {
            await foreach (var key in server.KeysAsync(pattern: $"mm:queue:{gameId.Value}:*").WithCancellation(cancellationToken))
            {
                var text = (string)key!;
                regions.Add(text[(text.LastIndexOf(':') + 1)..]);
            }

            await foreach (var key in server.KeysAsync(pattern: $"mm:open:{gameId.Value}:*").WithCancellation(cancellationToken))
            {
                var text = (string)key!;
                regions.Add(text[(text.LastIndexOf(':') + 1)..]);
            }
        }

        return regions
            .Select(r => MatchRegion.Create(r))
            .Where(r => r.IsSuccess)
            .Select(r => r.Value)
            .OrderBy(r => r.Value)
            .ToList();
    }
}
