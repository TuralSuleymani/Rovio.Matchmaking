namespace Rovio.Matchmaking.Application.Services.Implementations;

public sealed class GameConfigService(
    IGameConfigRepository repository,
    IGameConfigProjector projector,
    TimeProvider timeProvider) : IGameConfigService
{
    public async Task<Result<GameMatchConfigDto, IDomainError>> GetAsync(
        GameId gameId,
        CancellationToken cancellationToken = default)
    {
        var config = await repository.GetAsync(gameId, cancellationToken);
        if (config is null)
        {
            return Result.Failure<GameMatchConfigDto, IDomainError>(
                DomainError.NotFound($"Game '{gameId}' was not found.", "game_not_found"));
        }

        return config.ToDto();
    }

    public async Task<Result<IReadOnlyList<string>, IDomainError>> ListGameIdsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configs = await repository.ListAsync(cancellationToken);
            return Result.Success<IReadOnlyList<string>, IDomainError>(
                configs.Select(c => c.GameId.ToString()).OrderBy(id => id).ToList());
        }
        catch (Exception)
        {
            return Result.Failure<IReadOnlyList<string>, IDomainError>(
                DomainError.Unavailable("Config store is unavailable.", "postgres_unavailable"));
        }
    }

    public async Task<Result<GameMatchConfigDto, IDomainError>> UpsertAsync(
        GameId gameId,
        UpsertGameConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        GameMatchConfig? existing;
        try
        {
            existing = await repository.GetAsync(gameId, cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<GameMatchConfigDto, IDomainError>(
                DomainError.Unavailable("Config store is unavailable.", "postgres_unavailable"));
        }

        var now = timeProvider.GetUtcNow();
        LatencyPolicy latency;
        if (request.LatencyPolicy is null)
        {
            latency = existing?.LatencyPolicy ?? LatencyPolicy.Default;
        }
        else
        {
            var latencyResult = LatencyPolicy.Create(
                request.LatencyPolicy.BaseMaxLatencyDeltaMs,
                request.LatencyPolicy.ExpansionIntervalSeconds,
                request.LatencyPolicy.ExpansionStepMs,
                request.LatencyPolicy.AbsoluteMaxLatencyDeltaMs);
            if (latencyResult.IsFailure)
            {
                return Result.Failure<GameMatchConfigDto, IDomainError>(latencyResult.Error);
            }

            latency = latencyResult.Value;
        }

        var createResult = GameMatchConfig.Create(
            gameId,
            request.MinPlayers,
            request.MaxPlayers,
            request.AllowLateJoin,
            request.Enabled,
            request.MaxQueueDepth,
            latency,
            existing?.CreatedAt ?? now,
            now);

        if (createResult.IsFailure)
        {
            return Result.Failure<GameMatchConfigDto, IDomainError>(createResult.Error);
        }

        var config = createResult.Value;

        try
        {
            await repository.UpsertAsync(config, cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<GameMatchConfigDto, IDomainError>(
                DomainError.Unavailable("Config store is unavailable.", "postgres_unavailable"));
        }

        try
        {
            await projector.PublishAsync(config, cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<GameMatchConfigDto, IDomainError>(
                DomainError.Unavailable(
                    "Config saved to Postgres but Redis projection failed. It will be healed on startup re-project.",
                    "config_projection_failed"));
        }

        return config.ToDto();
    }

    public async Task<UnitResult<IDomainError>> EnsureSeededAndProjectedAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await repository.ListAsync(cancellationToken);
            if (all.Count == 0)
            {
                var seed = GameMatchConfig.CreateAngryBirds2Defaults(timeProvider.GetUtcNow());
                await repository.UpsertAsync(seed, cancellationToken);
                all = await repository.ListAsync(cancellationToken);
            }

            await projector.ProjectAllAsync(all, cancellationToken);
            return UnitResult.Success<IDomainError>();
        }
        catch (Exception)
        {
            return UnitResult.Failure<IDomainError>(
                DomainError.Unavailable(
                    "Failed to seed or project game configs.",
                    "config_bootstrap_failed"));
        }
    }
}
