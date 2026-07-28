using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Rovio.Matchmaking.Tests.Data.Fakes;
using StackExchange.Redis;

// AddMatchmakingInfrastructure lives in this namespace
using Rovio.Matchmaking.Infrastructure;

namespace Rovio.Matchmaking.Infrastructure.Tests.Integration.Fixtures;

public sealed class InfrastructureFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _postgres = new();
    private readonly RedisContainerFixture _redis = new();

    public IServiceProvider Services { get; private set; } = null!;
    public FixedTimeProvider Clock { get; private set; } = null!;
    public bool DockerAvailable { get; private set; }
    public Exception? StartupError { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = _postgres.ConnectionString,
                    ["ConnectionStrings:Redis"] = $"{_redis.ConnectionString},allowAdmin=true",
                    ["Matchmaking:MaxQueueDepth"] = DefaultOptionsMaxQueueDepth.ToString(),
                    ["Matchmaking:ShardLockSeconds"] = "5",
                    ["Matchmaking:MatchCandidateLimit"] = "200",
                    ["Matchmaking:SeedRegions:0"] = DefaultRegion,
                    ["Matchmaking:SeedRegions:1"] = NaRegion
                })
                .Build();

            Clock = new FixedTimeProvider(DefaultNow);

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            services.AddMatchmakingInfrastructure(configuration);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            Services = services.BuildServiceProvider(validateScopes: true);
            await PostgreSqlContainerFixture.RunMigrationAsync(Services);
            DockerAvailable = true;
        }
        catch (Exception ex)
        {
            StartupError = ex;
            DockerAvailable = false;
            Services = new ServiceCollection().BuildServiceProvider();
        }
    }

    public async Task DisposeAsync()
    {
        if (Services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (DockerAvailable)
        {
            await _postgres.DisposeAsync();
            await _redis.DisposeAsync();
        }
    }

    public async Task ClearPostgresAsync()
    {
        await _postgres.ClearGameConfigsAsync(Services);
    }

    public async Task FlushRedisAsync()
    {
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        await RedisContainerFixture.FlushAsync(redis);
    }

    public T Resolve<T>() where T : notnull =>
        Services.GetRequiredService<T>();

    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();
}
