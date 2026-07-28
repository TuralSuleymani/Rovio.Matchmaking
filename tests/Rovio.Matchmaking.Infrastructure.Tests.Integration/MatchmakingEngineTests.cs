using Microsoft.Extensions.DependencyInjection;

namespace Rovio.Matchmaking.Infrastructure.Tests.Integration;

public sealed class MatchmakingEngineTests(InfrastructureFixture fixture) : BaseInfrastructureSpec(fixture)
{
    [Fact]
    public async Task RunOnceAsync_WhenCompatibleQueuedPlayers_ShouldFormSession()
    {
        RequireDocker();
        var gameId = UniqueGameId("eng");
        var region = UniqueRegion();
        await SeedConfigAsync(gameId, minPlayers: DefaultMinPlayers, maxPlayers: DuoMaxPlayers);

        var ticketStore = Resolve<ITicketStore>();
        var sessionStore = Resolve<ISessionStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var t2 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("b"), region, CompatibleLatencyMs, ValidMaxQueueDepth, RecentEnqueueAt);

        await RunEngineOnceAsync();

        var ticket1 = await ticketStore.GetAsync(t1.Value.Ticket.Id);
        var ticket2 = await ticketStore.GetAsync(t2.Value.Ticket.Id);
        ticket1.Value.Status.Should().Be(TicketStatus.Matched);
        ticket2.Value.Status.Should().Be(TicketStatus.Matched);
        ticket1.Value.SessionId.Should().NotBeNull();
        ticket1.Value.SessionId.Should().Be(ticket2.Value.SessionId);

        var session = await sessionStore.GetAsync(ticket1.Value.SessionId!);
        session.IsSuccess.Should().BeTrue();
        session.Value.PlayerIds.Should().HaveCount(DuoMaxPlayers);
    }

    [Fact]
    public async Task RunOnceAsync_WhenBelowMinPlayers_ShouldNotFormSession()
    {
        RequireDocker();
        var gameId = UniqueGameId("below");
        var region = UniqueRegion();
        await SeedConfigAsync(gameId, minPlayers: DefaultMinPlayers, maxPlayers: DuoMaxPlayers);

        var ticketStore = Resolve<ITicketStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("solo"), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);

        await RunEngineOnceAsync();

        var ticket = await ticketStore.GetAsync(t1.Value.Ticket.Id);
        ticket.Value.Status.Should().Be(TicketStatus.Queued);
        ticket.Value.SessionId.Should().BeNull();
    }

    [Fact]
    public async Task RunOnceAsync_WhenGameDisabled_ShouldSkip()
    {
        RequireDocker();
        var gameId = UniqueGameId("off");
        var region = UniqueRegion();
        await SeedConfigAsync(
            gameId,
            minPlayers: DefaultMinPlayers,
            maxPlayers: DuoMaxPlayers,
            enabled: false);

        var ticketStore = Resolve<ITicketStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var t2 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("b"), region, CompatibleLatencyMs, ValidMaxQueueDepth, RecentEnqueueAt);

        await RunEngineOnceAsync();

        (await ticketStore.GetAsync(t1.Value.Ticket.Id)).Value.Status.Should().Be(TicketStatus.Queued);
        (await ticketStore.GetAsync(t2.Value.Ticket.Id)).Value.Status.Should().Be(TicketStatus.Queued);
    }

    [Fact]
    public async Task RunOnceAsync_WhenOpenSessionAllowsLateJoin_ShouldFillSlot()
    {
        RequireDocker();
        var gameId = UniqueGameId("late");
        var region = UniqueRegion();
        await SeedConfigAsync(
            gameId,
            minPlayers: DefaultMinPlayers,
            maxPlayers: TrioMaxPlayers,
            allowLateJoin: true);

        var ticketStore = Resolve<ITicketStore>();
        var sessionStore = Resolve<ISessionStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, ValidMaxQueueDepth, OldestEnqueueAt);
        var t2 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("b"), region, CompatibleLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);

        await RunEngineOnceAsync();

        var matched1 = await ticketStore.GetAsync(t1.Value.Ticket.Id);
        matched1.Value.Status.Should().Be(TicketStatus.Matched);
        var sessionId = matched1.Value.SessionId!;
        (await sessionStore.GetAsync(sessionId)).Value.OpenSlots.Should().Be(1);

        var t3 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("c"), region, CompatibleLatencyOffsetMs, ValidMaxQueueDepth, RecentEnqueueAt);

        await RunEngineOnceAsync();

        var lateTicket = await ticketStore.GetAsync(t3.Value.Ticket.Id);
        lateTicket.Value.Status.Should().Be(TicketStatus.Matched);
        lateTicket.Value.SessionId.Should().Be(sessionId);

        var session = await sessionStore.GetAsync(sessionId);
        session.Value.PlayerIds.Should().HaveCount(TrioMaxPlayers);
        session.Value.Status.Should().Be(SessionStatus.Full);
    }

    private async Task SeedConfigAsync(
        GameId gameId,
        int minPlayers,
        int maxPlayers,
        bool allowLateJoin = true,
        bool enabled = true)
    {
        var projector = Resolve<IGameConfigProjector>();
        var config = GameMatchConfigFactory.Create(
            gameId: gameId.Value,
            minPlayers: minPlayers,
            maxPlayers: maxPlayers,
            allowLateJoin: allowLateJoin,
            enabled: enabled,
            maxQueueDepth: ValidMaxQueueDepth);
        await projector.PublishAsync(config);
    }

    private async Task RunEngineOnceAsync()
    {
        await using var scope = Fixture.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IMatchmakingEngine>();
        await engine.RunOnceAsync();
    }
}
