namespace Rovio.Matchmaking.Application.Services.Contracts;

public interface IGameConfigService
{
    Task<UnitResult<IDomainError>> EnsureSeededAndProjectedAsync(CancellationToken cancellationToken = default);
    Task<Result<GameMatchConfigDto, IDomainError>> GetAsync(GameId gameId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<string>, IDomainError>> ListGameIdsAsync(CancellationToken cancellationToken = default);
    Task<Result<GameMatchConfigDto, IDomainError>> UpsertAsync(GameId gameId, UpsertGameConfigRequest request, CancellationToken cancellationToken = default);
}
