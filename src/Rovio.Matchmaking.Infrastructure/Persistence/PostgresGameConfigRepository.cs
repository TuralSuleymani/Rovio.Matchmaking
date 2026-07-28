namespace Rovio.Matchmaking.Infrastructure.Persistence;

public sealed class PostgresGameConfigRepository(MatchmakingDbContext db) : IGameConfigRepository
{
    public async Task<GameMatchConfig?> GetAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var entity = await db.GameMatchConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameId == gameId.Value, cancellationToken);
        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<GameMatchConfig>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await db.GameMatchConfigs.AsNoTracking().ToListAsync(cancellationToken);
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task UpsertAsync(GameMatchConfig config, CancellationToken cancellationToken = default)
    {
        var existing = await db.GameMatchConfigs.FirstOrDefaultAsync(x => x.GameId == config.GameId.Value, cancellationToken);
        if (existing is null)
        {
            db.GameMatchConfigs.Add(GameMatchConfigEntity.FromDomain(config));
        }
        else
        {
            existing.MinPlayers = config.PlayerCapacity.MinPlayerCount;
            existing.MaxPlayers = config.PlayerCapacity.MaxPlayerCount;
            existing.AllowLateJoin = config.AllowLateJoin;
            existing.Enabled = config.Enabled;
            existing.MaxQueueDepth = config.MaxQueueDepth;
            existing.LatencyPolicyJson = GameMatchConfigEntity.FromDomain(config).LatencyPolicyJson;
            existing.UpdatedAt = config.UpdatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
