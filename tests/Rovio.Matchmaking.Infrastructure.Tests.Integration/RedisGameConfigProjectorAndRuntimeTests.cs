
namespace Rovio.Matchmaking.Infrastructure.Tests.Integration;

public sealed class RedisGameConfigProjectorAndRuntimeTests(InfrastructureFixture fixture)
    : BaseInfrastructureSpec(fixture)
{
    [Fact]
    public async Task PublishAsync_WhenConfigPublished_ShouldBeReadableViaRuntime()
    {
        RequireDocker();
        var gameId = UniqueGameId("cfg");
        var config = GameMatchConfigFactory.Create(gameId: gameId.Value,
            minPlayers: DefaultMinPlayers,
            maxPlayers: TrioMaxPlayers,
            allowLateJoin: true,
            enabled: true,
            maxQueueDepth: ValidMaxQueueDepth,
            latencyPolicy: LatencyPolicyFactory.CreateDefault());

        var projector = Resolve<IGameConfigProjector>();
        var runtime = Resolve<IGameConfigRuntime>();

        await projector.PublishAsync(config);

        var loaded = await runtime.GetAsync(gameId);
        loaded.Should().NotBeNull();
        loaded!.GameId.Should().Be(gameId);
        loaded.PlayerCapacity.MinPlayerCount.Should().Be(DefaultMinPlayers);
        loaded.PlayerCapacity.MaxPlayerCount.Should().Be(TrioMaxPlayers);
        loaded.AllowLateJoin.Should().BeTrue();
        loaded.Enabled.Should().BeTrue();
        loaded.MaxQueueDepth.Should().Be(ValidMaxQueueDepth);
        loaded.LatencyPolicy.BaseMaximumDelta.Milliseconds.Should().Be(
            config.LatencyPolicy.BaseMaximumDelta.Milliseconds);
        loaded.LatencyPolicy.AbsoluteMaximumDelta.Milliseconds.Should().Be(
            config.LatencyPolicy.AbsoluteMaximumDelta.Milliseconds);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ShouldReturnNull()
    {
        RequireDocker();
        var runtime = Resolve<IGameConfigRuntime>();

        var loaded = await runtime.GetAsync(UniqueGameId("missing-cfg"));

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task ProjectAllAsync_WhenMultiple_ShouldIndexAndListGameIds()
    {
        RequireDocker();
        var firstId = UniqueGameId("all-a");
        var secondId = UniqueGameId("all-b");
        var configs = new[]
        {
            GameMatchConfigFactory.Create(gameId: firstId.Value),
            GameMatchConfigFactory.Create(gameId: secondId.Value, enabled: false)
        };

        var projector = Resolve<IGameConfigProjector>();
        var runtime = Resolve<IGameConfigRuntime>();

        await projector.ProjectAllAsync(configs);

        var gameIds = await runtime.ListGameIdsAsync();
        gameIds.Should().Contain(firstId);
        gameIds.Should().Contain(secondId);

        var second = await runtime.GetAsync(secondId);
        second.Should().NotBeNull();
        second!.Enabled.Should().BeFalse();
    }
}
