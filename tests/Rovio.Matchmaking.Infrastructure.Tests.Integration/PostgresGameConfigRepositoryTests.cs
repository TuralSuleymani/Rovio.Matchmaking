using Microsoft.Extensions.DependencyInjection;

namespace Rovio.Matchmaking.Infrastructure.Tests.Integration;

public sealed class PostgresGameConfigRepositoryTests(InfrastructureFixture fixture)
    : BaseInfrastructureSpec(fixture)
{
    [Fact]
    public async Task GetAsync_WhenConfigExists_ShouldReturnConfig()
    {
        RequireDocker();
        var gameId = UniqueGameId("pg-get");
        var config = GameMatchConfigFactory.Create(gameId: gameId.Value);

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            await repo.UpsertAsync(config);
        }

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            var loaded = await repo.GetAsync(gameId);
            loaded.Should().NotBeNull();
            loaded!.GameId.Should().Be(gameId);
            loaded.PlayerCapacity.MinPlayerCount.Should().Be(DefaultMinPlayers);
            loaded.PlayerCapacity.MaxPlayerCount.Should().Be(DefaultMaxPlayers);
            loaded.AllowLateJoin.Should().BeTrue();
            loaded.Enabled.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetAsync_WhenConfigMissing_ShouldReturnNull()
    {
        RequireDocker();
        await using var scope = Fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();

        var loaded = await repo.GetAsync(UniqueGameId("missing"));

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_WhenEmpty_ShouldReturnEmpty()
    {
        RequireDocker();
        await Fixture.ClearPostgresAsync();

        await using var scope = Fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();

        var list = await repo.ListAsync();

        list.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_WhenMultiple_ShouldReturnAll()
    {
        RequireDocker();
        await Fixture.ClearPostgresAsync();
        var firstId = UniqueGameId("list-a");
        var secondId = UniqueGameId("list-b");

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            await repo.UpsertAsync(GameMatchConfigFactory.Create(gameId: firstId.Value));
            await repo.UpsertAsync(GameMatchConfigFactory.Create(gameId: secondId.Value));
        }

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            var list = await repo.ListAsync();
            list.Select(c => c.GameId).Should().BeEquivalentTo([firstId, secondId]);
        }
    }

    [Fact]
    public async Task UpsertAsync_WhenNew_ShouldInsert()
    {
        RequireDocker();
        var gameId = UniqueGameId("pg-ins");
        var config = GameMatchConfigFactory.Create(gameId: gameId.Value,
            maxPlayers: TrioMaxPlayers,
            maxQueueDepth: ValidMaxQueueDepth);

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            await repo.UpsertAsync(config);
        }

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            var loaded = await repo.GetAsync(gameId);
            loaded.Should().NotBeNull();
            loaded!.PlayerCapacity.MaxPlayerCount.Should().Be(TrioMaxPlayers);
            loaded.MaxQueueDepth.Should().Be(ValidMaxQueueDepth);
        }
    }

    [Fact]
    public async Task UpsertAsync_WhenExisting_ShouldUpdate()
    {
        RequireDocker();
        var gameId = UniqueGameId("pg-upd");
        var initial = GameMatchConfigFactory.Create(gameId: gameId.Value,
            maxPlayers: DefaultMaxPlayers,
            allowLateJoin: true);

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            await repo.UpsertAsync(initial);
        }

        var updated = GameMatchConfigFactory.Create(gameId: gameId.Value,
            maxPlayers: DuoMaxPlayers,
            allowLateJoin: false,
            enabled: false,
            maxQueueDepth: ValidMaxQueueDepth,
            updatedAt: DefaultNow.AddMinutes(1));

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            await repo.UpsertAsync(updated);
        }

        await using (var scope = Fixture.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IGameConfigRepository>();
            var loaded = await repo.GetAsync(gameId);
            loaded.Should().NotBeNull();
            loaded!.PlayerCapacity.MaxPlayerCount.Should().Be(DuoMaxPlayers);
            loaded.AllowLateJoin.Should().BeFalse();
            loaded.Enabled.Should().BeFalse();
            loaded.MaxQueueDepth.Should().Be(ValidMaxQueueDepth);
            loaded.UpdatedAt.Should().Be(DefaultNow.AddMinutes(1));
        }
    }
}
