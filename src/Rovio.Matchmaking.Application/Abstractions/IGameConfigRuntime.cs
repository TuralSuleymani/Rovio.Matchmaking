namespace Rovio.Matchmaking.Application.Abstractions;

public interface IGameConfigRuntime
{
    Task<GameMatchConfig?> GetAsync(GameId gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameId>> ListGameIdsAsync(CancellationToken cancellationToken = default);
}
