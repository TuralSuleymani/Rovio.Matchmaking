using Rovio.Domain.Common;
using Rovio.Matchmaking.Domain.Entities;

namespace Rovio.Matchmaking.Infrastructure.Tests.Integration;

public sealed class RedisTicketStoreTests(InfrastructureFixture fixture) : BaseInfrastructureSpec(fixture)
{
    [Fact]
    public async Task EnqueueAsync_WhenNewPlayer_ShouldCreateQueuedTicket()
    {
        RequireDocker();
        var gameId = UniqueGameId("tq");
        var playerId = UniquePlayerId();
        var region = UniqueRegion();
        var store = Resolve<ITicketStore>();

        var result = await store.EnqueueAsync(
            gameId,
            playerId,
            region,
            DefaultLatencyMs,
            ValidMaxQueueDepth,
            OlderEnqueueAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeTrue();
        result.Value.Ticket.PlayerId.Should().Be(playerId);
        result.Value.Ticket.GameId.Should().Be(gameId);
        result.Value.Ticket.Region.Should().Be(region);
        result.Value.Ticket.Status.Should().Be(TicketStatus.Queued);
        result.Value.Ticket.Latency.Milliseconds.Should().Be(DefaultLatencyMs);
    }

    [Fact]
    public async Task EnqueueAsync_WhenSamePlayerQueued_ShouldReturnExisting()
    {
        RequireDocker();
        var gameId = UniqueGameId("idem");
        var playerId = UniquePlayerId();
        var region = UniqueRegion();
        var store = Resolve<ITicketStore>();

        var first = await store.EnqueueAsync(
            gameId, playerId, region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var second = await store.EnqueueAsync(
            gameId, playerId, region, CompatibleLatencyMs, ValidMaxQueueDepth, RecentEnqueueAt);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.Created.Should().BeFalse();
        second.Value.Ticket.Id.Should().Be(first.Value.Ticket.Id);
        second.Value.Ticket.Latency.Milliseconds.Should().Be(DefaultLatencyMs);
    }

    [Fact]
    public async Task EnqueueAsync_WhenQueueFull_ShouldFail()
    {
        RequireDocker();
        var gameId = UniqueGameId("full");
        var region = UniqueRegion();
        var store = Resolve<ITicketStore>();
        const int maxDepth = 1;

        var first = await store.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, maxDepth, OlderEnqueueAt);
        first.IsSuccess.Should().BeTrue();

        var second = await store.EnqueueAsync(
            gameId, UniquePlayerId("b"), region, DefaultLatencyMs, maxDepth, RecentEnqueueAt);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be(ErrorCodes.QueueFull);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ShouldFail()
    {
        RequireDocker();
        var store = Resolve<ITicketStore>();

        var result = await store.GetAsync(Id<MatchTicket>.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task CancelAsync_WhenQueued_ShouldSucceed()
    {
        RequireDocker();
        var gameId = UniqueGameId("cancel");
        var region = UniqueRegion();
        var store = Resolve<ITicketStore>();
        var enqueued = await store.EnqueueAsync(
            gameId, UniquePlayerId(), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);

        var cancel = await store.CancelAsync(gameId, enqueued.Value.Ticket.Id);

        cancel.IsSuccess.Should().BeTrue();
        var loaded = await store.GetAsync(enqueued.Value.Ticket.Id);
        loaded.IsSuccess.Should().BeTrue();
        loaded.Value.Status.Should().Be(TicketStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadyCancelledOrMatched_ShouldFail()
    {
        RequireDocker();
        var gameId = UniqueGameId("cancel2");
        var region = UniqueRegion();
        var store = Resolve<ITicketStore>();
        var enqueued = await store.EnqueueAsync(
            gameId, UniquePlayerId(), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);

        (await store.CancelAsync(gameId, enqueued.Value.Ticket.Id)).IsSuccess.Should().BeTrue();
        var secondCancel = await store.CancelAsync(gameId, enqueued.Value.Ticket.Id);

        secondCancel.IsFailure.Should().BeTrue();
        secondCancel.Error.Code.Should().Be(ErrorCodes.NotQueued);
    }

    [Fact]
    public async Task GetQueuedCandidatesAsync_WhenTicketsExist_ShouldReturnOrderedByEnqueueTime()
    {
        RequireDocker();
        var gameId = UniqueGameId("cand");
        var region = UniqueRegion();
        var store = Resolve<ITicketStore>();

        var older = await store.EnqueueAsync(
            gameId, UniquePlayerId("old"), region, DefaultLatencyMs, ValidMaxQueueDepth, OldestEnqueueAt);
        var newer = await store.EnqueueAsync(
            gameId, UniquePlayerId("new"), region, CompatibleLatencyMs, ValidMaxQueueDepth, RecentEnqueueAt);

        var candidates = await store.GetQueuedCandidatesAsync(gameId, region, limit: 10);

        candidates.IsSuccess.Should().BeTrue();
        candidates.Value.Should().HaveCount(2);
        candidates.Value[0].Id.Should().Be(older.Value.Ticket.Id);
        candidates.Value[1].Id.Should().Be(newer.Value.Ticket.Id);
    }
}
