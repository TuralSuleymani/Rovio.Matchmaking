namespace Rovio.Matchmaking.Application.Abstractions;

public interface IGameConfigProjector
{
    Task PublishAsync(GameMatchConfig config, CancellationToken cancellationToken = default);
    Task ProjectAllAsync(IEnumerable<GameMatchConfig> configs, CancellationToken cancellationToken = default);
}
