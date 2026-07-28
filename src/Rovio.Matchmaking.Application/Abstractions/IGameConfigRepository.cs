namespace Rovio.Matchmaking.Application.Abstractions;

public interface IGameConfigRepository
{
    Task<GameMatchConfig?> GetAsync(GameId gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameMatchConfig>> ListAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(GameMatchConfig config, CancellationToken cancellationToken = default);
}
