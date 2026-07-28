namespace Rovio.Matchmaking.Infrastructure.Redis;

public sealed class RedisGameConfigRuntime(IConnectionMultiplexer redis) : IGameConfigRuntime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GameMatchConfig?> GetAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(RedisKeys.Config(gameId.Value)).WaitAsync(cancellationToken);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        var projection = JsonSerializer.Deserialize<GameMatchConfigProjection>((string)value!, JsonOptions);
        return projection?.ToDomain();
    }

    public async Task<IReadOnlyList<GameId>> ListGameIdsAsync(CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var members = await db.SetMembersAsync(RedisKeys.ConfigIndex).WaitAsync(cancellationToken);
        return members
            .Select(m => GameId.Create((string)m!).Value)
            .OrderBy(id => id.Value)
            .ToList();
    }
}
