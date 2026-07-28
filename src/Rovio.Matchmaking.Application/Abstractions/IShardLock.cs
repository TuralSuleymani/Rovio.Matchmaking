namespace Rovio.Matchmaking.Application.Abstractions;

public interface IShardLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        GameId gameId,
        MatchRegion region,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);
}
