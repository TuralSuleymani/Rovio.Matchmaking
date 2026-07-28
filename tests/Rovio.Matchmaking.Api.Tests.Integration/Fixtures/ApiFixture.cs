using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rovio.Matchmaking.Application.Abstractions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Rovio.Matchmaking.Api.Tests.Integration.Fixtures;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("matchmaking")
        .WithUsername("matchmaking")
        .WithPassword("matchmaking")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public HttpClient Client { get; private set; } = null!;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public bool DockerAvailable { get; private set; }
    public Exception? StartupError { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
            DockerAvailable = true;
        }
        catch (Exception ex)
        {
            StartupError = ex;
            DockerAvailable = false;
            Client = new HttpClient();
            Factory = null!;
            return;
        }

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
                builder.UseSetting(
                    "ConnectionStrings:Redis",
                    $"{_redis.GetConnectionString()},abortConnect=false");
                builder.UseSetting("Matchmaking:MaxQueueDepth", "3");
                builder.UseSetting("Matchmaking:ShardLockSeconds", "5");
                builder.UseSetting("Matchmaking:MatchCandidateLimit", "200");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                        ["ConnectionStrings:Redis"] = $"{_redis.GetConnectionString()},abortConnect=false",
                        ["Matchmaking:MaxQueueDepth"] = "3",
                        ["Matchmaking:ShardLockSeconds"] = "5",
                        ["Matchmaking:MatchCandidateLimit"] = "200"
                    });
                });
            });

        try
        {
            Client = Factory.CreateClient();
        }
        catch (Exception ex)
        {
            StartupError = ex;
            DockerAvailable = false;
            Client = new HttpClient();
        }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (DockerAvailable)
        {
            await _postgres.DisposeAsync();
            await _redis.DisposeAsync();
        }
    }

    public async Task RunMatchOnceAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IMatchmakingEngine>();
        await engine.RunOnceAsync();
    }
}
