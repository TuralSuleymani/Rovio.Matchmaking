using Rovio.Matchmaking.Application.Models;
using Rovio.Matchmaking.Application.Models.Dtos;
using Rovio.Matchmaking.Application.Services.Contracts;

namespace Rovio.Matchmaking.Application.Tests.Unit.Services;

public sealed class SessionServiceTests
{
    private readonly ISessionStore _sessionStore = Substitute.For<ISessionStore>();
    private readonly ITicketStore _ticketStore = Substitute.For<ITicketStore>();
    private readonly IGameConfigRuntime _configRuntime = Substitute.For<IGameConfigRuntime>();
    private readonly IQueueService _queueService = Substitute.For<IQueueService>();
    private readonly SessionService _sut;

    public SessionServiceTests()
    {
        _sut = new SessionService(
            _sessionStore,
            _ticketStore,
            _configRuntime,
            _queueService);
    }

    [Fact]
    public async Task GetAsync_WhenSessionExists_ShouldReturnDto()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));

        // Act
        var result = await _sut.GetAsync(session.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be(session.Id.ToString());
        result.Value.GameId.Should().Be(AngryBirds2GameId);
        result.Value.Region.Should().Be(DefaultRegion);
    }

    [Fact]
    public async Task GetAsync_WhenSessionStoreFails_ShouldFail()
    {
        // Arrange
        var sessionId = IdFactory.CreateNew<GameSession>();
        _sessionStore.GetAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<GameSession, IDomainError>(
                DomainError.NotFound(code: ErrorCodes.SessionNotFound)));

        // Act
        var result = await _sut.GetAsync(sessionId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.SessionNotFound);
    }

    [Fact]
    public async Task LateJoinAsync_WhenSessionAcceptsPlayer_ShouldReturnUpdatedSession()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var updatedSession = GameSessionFactory.CreateWithOpenSlot();
        var ticket = MatchTicketFactory.CreateQueued(playerId: ThirdPlayerId);
        var ticketDto = new TicketDto(
            ticket.Id.ToString(),
            ThirdPlayerId,
            AngryBirds2GameId,
            DefaultRegion,
            DefaultLatencyMs,
            ticket.EnqueuedAt,
            TicketStatus.Queued.Name,
            SessionId: null);
        var request = LateJoinRequestFactory.Create();

        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(
                Result.Success<GameSession, IDomainError>(session),
                Result.Success<GameSession, IDomainError>(updatedSession));
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns(GameMatchConfigFactory.Create());
        _queueService.EnqueueAsync(Arg.Is<GameId>(g => g.Value == AngryBirds2GameId),
                Arg.Is<EnqueueRequest>(r =>
                    r.PlayerId == ThirdPlayerId &&
                    r.Region == DefaultRegion &&
                    r.LatencyMs == DefaultLatencyMs),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<(TicketDto Ticket, bool Created), IDomainError>((ticketDto, true)));
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));
        _sessionStore.LateJoinAsync(
                session.Id,
                GameIdFactory.Create(),
                MatchRegionFactory.Create(),
                ticket.Id,
                PlayerIdFactory.CreateThird(),
                Arg.Any<CancellationToken>())
            .Returns(UnitResult.Success<IDomainError>());

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be(updatedSession.Id.ToString());
        await _sessionStore.Received(1).LateJoinAsync(
            session.Id,
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            ticket.Id,
            PlayerIdFactory.CreateThird(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LateJoinAsync_WhenLateJoinIsDisabled_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot(allowLateJoin: false);
        var request = LateJoinRequestFactory.Create();
        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.LateJoinDisabled);
    }

    [Fact]
    public async Task LateJoinAsync_WhenSessionIsFull_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.Rehydrate(
            SessionStatus.Full,
            allowLateJoin: true,
            playerCapacity: PlayerCapacityFactory.CreateDuo(),
            playerIds:
            [
                PlayerIdFactory.Create(),
                PlayerIdFactory.CreateSecond()
            ]);
        var request = LateJoinRequestFactory.Create();
        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.SessionFull);
    }

    [Fact]
    public async Task LateJoinAsync_WhenGameConfigIsMissing_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var request = LateJoinRequestFactory.Create();
        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.GameNotFound);
    }

    [Fact]
    public async Task LateJoinAsync_WhenRuntimeThrows_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var request = LateJoinRequestFactory.Create();
        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.RedisUnavailable);
    }

    [Fact]
    public async Task LateJoinAsync_WhenRegionDoesNotMatch_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var request = LateJoinRequestFactory.Create(region: NaRegion);
        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns(GameMatchConfigFactory.Create());

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.RegionMismatch);
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public async Task LateJoinAsync_WhenEnqueueFails_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var request = LateJoinRequestFactory.Create();
        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns(GameMatchConfigFactory.Create());
        _queueService.EnqueueAsync(Arg.Is<GameId>(g => g.Value == AngryBirds2GameId),
                Arg.Any<EnqueueRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure<(TicketDto Ticket, bool Created), IDomainError>(
                DomainError.TooManyRequests(code: ErrorCodes.QueueFull)));

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.QueueFull);
    }

    [Fact]
    public async Task LateJoinAsync_WhenTicketAlreadyMatchedToSession_ShouldReturnRefreshedSession()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var ticket = MatchTicketFactory.RehydrateMatched(session.Id, playerId: ThirdPlayerId);
        var ticketDto = new TicketDto(
            ticket.Id.ToString(),
            ThirdPlayerId,
            AngryBirds2GameId,
            DefaultRegion,
            DefaultLatencyMs,
            ticket.EnqueuedAt,
            TicketStatus.Matched.Name,
            session.Id.ToString());
        var request = LateJoinRequestFactory.Create();

        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns(GameMatchConfigFactory.Create());
        _queueService.EnqueueAsync(Arg.Is<GameId>(g => g.Value == AngryBirds2GameId),
                Arg.Any<EnqueueRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<(TicketDto Ticket, bool Created), IDomainError>((ticketDto, false)));
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be(session.Id.ToString());
        await _sessionStore.DidNotReceive().LateJoinAsync(
            Arg.Any<Id<GameSession>>(),
            Arg.Any<GameId>(),
            Arg.Any<MatchRegion>(),
            Arg.Any<Id<MatchTicket>>(),
            Arg.Any<PlayerId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LateJoinAsync_WhenTicketIsNotQueued_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var otherSessionId = IdFactory.CreateNew<GameSession>();
        var ticket = MatchTicketFactory.RehydrateMatched(otherSessionId, playerId: ThirdPlayerId);
        var ticketDto = new TicketDto(
            ticket.Id.ToString(),
            ThirdPlayerId,
            AngryBirds2GameId,
            DefaultRegion,
            DefaultLatencyMs,
            ticket.EnqueuedAt,
            TicketStatus.Matched.Name,
            otherSessionId.ToString());
        var request = LateJoinRequestFactory.Create();

        _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<GameSession, IDomainError>(session));
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns(GameMatchConfigFactory.Create());
        _queueService.EnqueueAsync(Arg.Is<GameId>(g => g.Value == AngryBirds2GameId),
                Arg.Any<EnqueueRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<(TicketDto Ticket, bool Created), IDomainError>((ticketDto, false)));
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));

        // Act
        var result = await _sut.LateJoinAsync(session.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TicketNotQueued);
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
    }
}
