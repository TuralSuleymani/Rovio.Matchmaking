namespace Rovio.Matchmaking.Infrastructure.Redis;

public sealed class RedisShardLock(IConnectionMultiplexer redis) : IShardLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        GameId gameId,
        MatchRegion region,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = RedisKeys.Lock(gameId.Value, region.Value);
        var token = Guid.NewGuid().ToString("N");
        var acquired = await db.StringSetAsync(key, token, ttl, When.NotExists).WaitAsync(cancellationToken);
        if (!acquired)
        {
            return null;
        }

        return new Releaser(db, key, token);
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private static readonly LuaScript ReleaseScript = LuaScript.Prepare(@"
            if redis.call('get', @key) == @token then
              return redis.call('del', @key)
            end
            return 0");

        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;
        private bool _disposed;

        public Releaser(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _db.ScriptEvaluateAsync(ReleaseScript, new { key = (RedisKey)_key, token = (RedisValue)_token });
        }
    }
}
