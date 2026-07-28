namespace Rovio.Matchmaking.Infrastructure.Redis;

public sealed class RedisGameConfigProjector(IConnectionMultiplexer redis) : IGameConfigProjector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(GameMatchConfig config, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(GameMatchConfigProjection.FromDomain(config), JsonOptions);
        var batch = db.CreateBatch();
        var setTask = batch.StringSetAsync(RedisKeys.Config(config.GameId.Value), json);
        var saddTask = batch.SetAddAsync(RedisKeys.ConfigIndex, config.GameId.Value);
        batch.Execute();
        await Task.WhenAll(setTask, saddTask).WaitAsync(cancellationToken);
    }

    public async Task ProjectAllAsync(IEnumerable<GameMatchConfig> configs, CancellationToken cancellationToken = default)
    {
        foreach (var config in configs)
        {
            await PublishAsync(config, cancellationToken);
        }
    }
}
