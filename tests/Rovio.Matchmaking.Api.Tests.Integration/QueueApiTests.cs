
namespace Rovio.Matchmaking.Api.Tests.Integration;

public sealed class QueueApiTests(ApiFixture fixture) : BaseApiSpec(fixture)
{
    [Fact]
    public async Task Enqueue_WhenNewPlayer_ShouldCreateTicket()
    {
        RequireDocker();
        var gameId = UniqueGameId("enq");
        var playerId = UniquePlayerId();
        var region = UniqueRegion();
        await EnsureGameConfigAsync(gameId);

        var (response, ticket) = await EnqueueAsync(
            gameId,
            playerId: playerId,
            region: region,
            latencyMs: DefaultLatencyMs);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ticket.Should().NotBeNull();
        ticket!.PlayerId.Should().Be(playerId);
        ticket.GameId.Should().Be(gameId);
        ticket.Region.Should().Be(region);
        ticket.Status.Should().Be(TicketStatus.Queued.Name);
    }

    [Fact]
    public async Task Enqueue_WhenSamePlayerQueued_ShouldReturnExisting()
    {
        RequireDocker();
        var gameId = UniqueGameId("idem");
        var playerId = UniquePlayerId();
        var region = UniqueRegion();
        await EnsureGameConfigAsync(gameId);

        var first = await EnqueueSuccessAsync(
            gameId, playerId: playerId, region: region, latencyMs: DefaultLatencyMs);
        var (secondResponse, second) = await EnqueueAsync(
            gameId, playerId: playerId, region: region, latencyMs: CompatibleLatencyMs);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Should().NotBeNull();
        second!.TicketId.Should().Be(first.TicketId);
        second.LatencyMs.Should().Be(DefaultLatencyMs);
    }

    [Fact]
    public async Task Enqueue_WhenQueueFull_ShouldReturn429()
    {
        RequireDocker();
        var gameId = UniqueGameId("full");
        var region = UniqueRegion();
        await EnsureGameConfigAsync(gameId, maxQueueDepth: null);

        await EnqueueSuccessAsync(gameId, UniquePlayerId("a"), region, DefaultLatencyMs);
        await EnqueueSuccessAsync(gameId, UniquePlayerId("b"), region, CompatibleLatencyMs);
        await EnqueueSuccessAsync(gameId, UniquePlayerId("c"), region, CompatibleLatencyOffsetMs);

        var (overflow, _) = await EnqueueAsync(
            gameId,
            UniquePlayerId("d"),
            region,
            IncompatibleLatencyMs);

        overflow.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Cancel_WhenQueued_ShouldMarkCancelled()
    {
        RequireDocker();
        var gameId = UniqueGameId("cancel");
        var region = UniqueRegion();
        await EnsureGameConfigAsync(gameId);

        var ticket = await EnqueueSuccessAsync(
            gameId, UniquePlayerId(), region, DefaultLatencyMs);

        var cancel = await Client.DeleteAsync($"/api/v1/games/{gameId}/queue/{ticket.TicketId}");

        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var loaded = await GetTicketAsync(gameId, ticket.TicketId);
        loaded.Status.Should().Be(TicketStatus.Cancelled.Name);
    }
}
