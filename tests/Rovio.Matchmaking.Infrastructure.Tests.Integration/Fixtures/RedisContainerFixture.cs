using StackExchange.Redis;
using Testcontainers.Redis;

namespace Rovio.Matchmaking.Infrastructure.Tests.Integration.Fixtures;

public sealed class RedisContainerFixture : IAsyncDisposable
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();

    public string ConnectionString => $"{_container.GetConnectionString()},abortConnect=false";

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }

    public static async Task FlushAsync(IConnectionMultiplexer redis)
    {
        var server = redis.GetServers().First(s => s.IsConnected);
        await server.FlushDatabaseAsync();
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
