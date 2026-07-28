using Rovio.Domain.Common;
using Rovio.Matchmaking.Domain.Entities;

namespace Rovio.Matchmaking.Infrastructure.Tests.Integration;

public sealed class RedisSessionStoreTests(InfrastructureFixture fixture) : BaseInfrastructureSpec(fixture)
{
    [Fact]
    public async Task FormSessionAsync_WhenTicketsQueued_ShouldCreateSessionAndMarkMatched()
    {
        RequireDocker();
        var gameId = UniqueGameId("sess");
        var region = UniqueRegion();
        var ticketStore = Resolve<ITicketStore>();
        var sessionStore = Resolve<ISessionStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var t2 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("b"), region, CompatibleLatencyMs, ValidMaxQueueDepth, RecentEnqueueAt);

        var session = GameSession.Create(
            gameId,
            region,
            PlayerCapacityFactory.CreateDuo(),
            allowLateJoin: true,
            [t1.Value.Ticket.PlayerId, t2.Value.Ticket.PlayerId],
            DefaultNow).Value;

        var form = await sessionStore.FormSessionAsync(
            session,
            [t1.Value.Ticket.Id, t2.Value.Ticket.Id]);

        form.IsSuccess.Should().BeTrue();

        var loaded = await sessionStore.GetAsync(session.Id);
        loaded.IsSuccess.Should().BeTrue();
        loaded.Value.PlayerIds.Should().HaveCount(DuoMaxPlayers);

        var ticket1 = await ticketStore.GetAsync(t1.Value.Ticket.Id);
        ticket1.Value.Status.Should().Be(TicketStatus.Matched);
        ticket1.Value.SessionId.Should().Be(session.Id);

        var open = await sessionStore.GetOpenSessionsAsync(gameId, region);
        open.IsSuccess.Should().BeTrue();
        open.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task FormSessionAsync_WhenTicketNotQueued_ShouldFail()
    {
        RequireDocker();
        var gameId = UniqueGameId("race");
        var region = UniqueRegion();
        var ticketStore = Resolve<ITicketStore>();
        var sessionStore = Resolve<ISessionStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var missingTicketId = Id<MatchTicket>.New();

        var session = GameSession.Create(
            gameId,
            region,
            PlayerCapacityFactory.CreateDuo(),
            allowLateJoin: false,
            [t1.Value.Ticket.PlayerId, PlayerIdFactory.CreateSecond()],
            DefaultNow).Value;

        var form = await sessionStore.FormSessionAsync(session, [t1.Value.Ticket.Id, missingTicketId]);

        form.IsFailure.Should().BeTrue();
        form.Error.Code.Should().Be(ErrorCodes.MatchRace);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ShouldFail()
    {
        RequireDocker();
        var sessionStore = Resolve<ISessionStore>();

        var result = await sessionStore.GetAsync(Id<GameSession>.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.SessionNotFound);
    }

    [Fact]
    public async Task GetOpenSessionsAsync_WhenLateJoinAllowed_ShouldReturnOpenSessions()
    {
        RequireDocker();
        var gameId = UniqueGameId("open");
        var region = UniqueRegion();
        var ticketStore = Resolve<ITicketStore>();
        var sessionStore = Resolve<ISessionStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var t2 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("b"), region, CompatibleLatencyMs, ValidMaxQueueDepth, RecentEnqueueAt);

        var session = GameSession.Create(
            gameId,
            region,
            PlayerCapacityFactory.CreateTrio(),
            allowLateJoin: true,
            [t1.Value.Ticket.PlayerId, t2.Value.Ticket.PlayerId],
            DefaultNow).Value;

        (await sessionStore.FormSessionAsync(session, [t1.Value.Ticket.Id, t2.Value.Ticket.Id]))
            .IsSuccess.Should().BeTrue();

        var open = await sessionStore.GetOpenSessionsAsync(gameId, region);

        open.IsSuccess.Should().BeTrue();
        open.Value.Should().ContainSingle(s => s.Id.Equals(session.Id));
        open.Value[0].OpenSlots.Should().Be(1);
    }

    [Fact]
    public async Task LateJoinAsync_WhenOpenSlot_ShouldSucceed()
    {
        RequireDocker();
        var gameId = UniqueGameId("lj");
        var region = UniqueRegion();
        var ticketStore = Resolve<ITicketStore>();
        var sessionStore = Resolve<ISessionStore>();

        var t1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("a"), region, DefaultLatencyMs, ValidMaxQueueDepth, OldestEnqueueAt);
        var t2 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("b"), region, CompatibleLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var t3 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("c"), region, CompatibleLatencyOffsetMs, ValidMaxQueueDepth, RecentEnqueueAt);

        var session = GameSession.Create(
            gameId,
            region,
            PlayerCapacityFactory.CreateTrio(),
            allowLateJoin: true,
            [t1.Value.Ticket.PlayerId, t2.Value.Ticket.PlayerId],
            DefaultNow).Value;

        (await sessionStore.FormSessionAsync(session, [t1.Value.Ticket.Id, t2.Value.Ticket.Id]))
            .IsSuccess.Should().BeTrue();

        var lateJoin = await sessionStore.LateJoinAsync(
            session.Id,
            gameId,
            region,
            t3.Value.Ticket.Id,
            t3.Value.Ticket.PlayerId);

        lateJoin.IsSuccess.Should().BeTrue();

        var loaded = await sessionStore.GetAsync(session.Id);
        loaded.Value.PlayerIds.Should().HaveCount(TrioMaxPlayers);
        loaded.Value.Status.Should().Be(SessionStatus.Full);

        var ticket = await ticketStore.GetAsync(t3.Value.Ticket.Id);
        ticket.Value.Status.Should().Be(TicketStatus.Matched);
    }

    [Fact]
    public async Task LateJoinAsync_WhenFullOrDisabled_ShouldFail()
    {
        RequireDocker();
        var gameId = UniqueGameId("ljfail");
        var region = UniqueRegion();
        var ticketStore = Resolve<ITicketStore>();
        var sessionStore = Resolve<ISessionStore>();

        var disabledT1 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("d1"), region, DefaultLatencyMs, ValidMaxQueueDepth, OldestEnqueueAt);
        var disabledT2 = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("d2"), region, CompatibleLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var disabledLate = await ticketStore.EnqueueAsync(
            gameId, UniquePlayerId("d3"), region, CompatibleLatencyOffsetMs, ValidMaxQueueDepth, RecentEnqueueAt);

        var disabledSession = GameSession.Create(
            gameId,
            region,
            PlayerCapacityFactory.CreateTrio(),
            allowLateJoin: false,
            [disabledT1.Value.Ticket.PlayerId, disabledT2.Value.Ticket.PlayerId],
            DefaultNow).Value;

        (await sessionStore.FormSessionAsync(
                disabledSession,
                [disabledT1.Value.Ticket.Id, disabledT2.Value.Ticket.Id]))
            .IsSuccess.Should().BeTrue();

        var disabledJoin = await sessionStore.LateJoinAsync(
            disabledSession.Id,
            gameId,
            region,
            disabledLate.Value.Ticket.Id,
            disabledLate.Value.Ticket.PlayerId);

        disabledJoin.IsFailure.Should().BeTrue();
        disabledJoin.Error.Code.Should().Be(ErrorCodes.LateJoinDisabled);

        var fullGameId = UniqueGameId("ljfull");
        var fullRegion = UniqueRegion();
        var fullT1 = await ticketStore.EnqueueAsync(
            fullGameId, UniquePlayerId("f1"), fullRegion, DefaultLatencyMs, ValidMaxQueueDepth, OldestEnqueueAt);
        var fullT2 = await ticketStore.EnqueueAsync(
            fullGameId, UniquePlayerId("f2"), fullRegion, CompatibleLatencyMs, ValidMaxQueueDepth, OlderEnqueueAt);
        var fullFill = await ticketStore.EnqueueAsync(
            fullGameId, UniquePlayerId("f3"), fullRegion, CompatibleLatencyOffsetMs, ValidMaxQueueDepth, RecentEnqueueAt);
        var fullOverflow = await ticketStore.EnqueueAsync(
            fullGameId,
            UniquePlayerId("f4"),
            fullRegion,
            CompatibleLatencyMs,
            ValidMaxQueueDepth,
            RecentEnqueueAt.AddSeconds(1));

        var openSession = GameSession.Create(
            fullGameId,
            fullRegion,
            PlayerCapacityFactory.CreateTrio(),
            allowLateJoin: true,
            [fullT1.Value.Ticket.PlayerId, fullT2.Value.Ticket.PlayerId],
            DefaultNow).Value;

        (await sessionStore.FormSessionAsync(openSession, [fullT1.Value.Ticket.Id, fullT2.Value.Ticket.Id]))
            .IsSuccess.Should().BeTrue();

        (await sessionStore.LateJoinAsync(
                openSession.Id,
                fullGameId,
                fullRegion,
                fullFill.Value.Ticket.Id,
                fullFill.Value.Ticket.PlayerId))
            .IsSuccess.Should().BeTrue();

        var fullJoin = await sessionStore.LateJoinAsync(
            openSession.Id,
            fullGameId,
            fullRegion,
            fullOverflow.Value.Ticket.Id,
            fullOverflow.Value.Ticket.PlayerId);

        fullJoin.IsFailure.Should().BeTrue();
        fullJoin.Error.Code.Should().Be(ErrorCodes.SessionFull);
    }
}
