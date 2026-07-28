
namespace Rovio.Matchmaking.Infrastructure.Tests.Integration;

public sealed class RedisShardLockTests(InfrastructureFixture fixture) : BaseInfrastructureSpec(fixture)
{
    [Fact]
    public async Task TryAcquireAsync_WhenFree_ShouldReturnLease()
    {
        RequireDocker();
        var lockService = Resolve<IShardLock>();
        var gameId = UniqueGameId("lock");
        var region = UniqueRegion();

        await using var lease = await lockService.TryAcquireAsync(
            gameId, region, TimeSpan.FromSeconds(5));

        lease.Should().NotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenHeld_ShouldReturnNull()
    {
        RequireDocker();
        var lockService = Resolve<IShardLock>();
        var gameId = UniqueGameId("held");
        var region = UniqueRegion();

        await using var first = await lockService.TryAcquireAsync(
            gameId, region, TimeSpan.FromSeconds(5));
        first.Should().NotBeNull();

        var second = await lockService.TryAcquireAsync(
            gameId, region, TimeSpan.FromSeconds(5));

        second.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_WhenReleased_ShouldAllowReacquire()
    {
        RequireDocker();
        var lockService = Resolve<IShardLock>();
        var gameId = UniqueGameId("rel");
        var region = UniqueRegion();

        var first = await lockService.TryAcquireAsync(gameId, region, TimeSpan.FromSeconds(5));
        first.Should().NotBeNull();
        await first!.DisposeAsync();

        await using var second = await lockService.TryAcquireAsync(
            gameId, region, TimeSpan.FromSeconds(5));

        second.Should().NotBeNull();
    }
}
