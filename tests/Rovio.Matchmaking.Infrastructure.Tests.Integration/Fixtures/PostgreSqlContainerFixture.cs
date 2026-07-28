using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rovio.Matchmaking.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Rovio.Matchmaking.Infrastructure.Tests.Integration.Fixtures;

public sealed class PostgreSqlContainerFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("matchmaking")
        .WithUsername("matchmaking")
        .WithPassword("matchmaking")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }

    public static async Task RunMigrationAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task ClearGameConfigsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE game_match_configs;");
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
