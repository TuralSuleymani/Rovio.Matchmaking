
namespace Rovio.Matchmaking.Application.Tests.Unit.Services;

public sealed class QueueServiceTests
{
    private readonly ITicketStore _ticketStore = Substitute.For<ITicketStore>();
    private readonly IGameConfigRuntime _configRuntime = Substitute.For<IGameConfigRuntime>();
    private readonly FixedTimeProvider _timeProvider = new();
    private readonly QueueService _sut;

    public QueueServiceTests()
    {
        _sut = new QueueService(
            _ticketStore,
            _configRuntime,
            _timeProvider,
            MatchmakingOptionsFactory.CreateOptions());
    }

    [Fact]
    public async Task EnqueueAsync_WhenGameIsEnabled_ShouldReturnTicket()
    {
        // Arrange
        var config = GameMatchConfigFactory.Create(maxQueueDepth: ValidMaxQueueDepth);
        var ticket = MatchTicketFactory.CreateQueued();
        var request = EnqueueRequestFactory.Create();
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>()).Returns(config);
        _ticketStore.EnqueueAsync(
                GameIdFactory.Create(),
                PlayerIdFactory.Create(),
                MatchRegionFactory.Create(),
                DefaultLatencyMs,
                ValidMaxQueueDepth,
                DefaultNow,
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<EnqueueResult, IDomainError>(new EnqueueResult(ticket, Created: true)));

        // Act
        var result = await _sut.EnqueueAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeTrue();
        result.Value.Ticket.PlayerId.Should().Be(DefaultPlayerId);
        result.Value.Ticket.GameId.Should().Be(AngryBirds2GameId);
    }

    [Fact]
    public async Task EnqueueAsync_WhenMaxQueueDepthIsMissing_ShouldUseOptionsDefault()
    {
        // Arrange
        var config = GameMatchConfigFactory.Create(maxQueueDepth: null);
        var ticket = MatchTicketFactory.CreateQueued();
        var request = EnqueueRequestFactory.Create();
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>()).Returns(config);
        _ticketStore.EnqueueAsync(
                GameIdFactory.Create(),
                PlayerIdFactory.Create(),
                MatchRegionFactory.Create(),
                DefaultLatencyMs,
                DefaultOptionsMaxQueueDepth,
                DefaultNow,
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<EnqueueResult, IDomainError>(new EnqueueResult(ticket, Created: false)));

        // Act
        var result = await _sut.EnqueueAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeFalse();
        await _ticketStore.Received(1).EnqueueAsync(
            GameIdFactory.Create(),
            PlayerIdFactory.Create(),
            MatchRegionFactory.Create(),
            DefaultLatencyMs,
            DefaultOptionsMaxQueueDepth,
            DefaultNow,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueAsync_WhenGameDoesNotExist_ShouldFail()
    {
        // Arrange
        var request = EnqueueRequestFactory.Create();
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .Returns((GameMatchConfig?)null);

        // Act
        var result = await _sut.EnqueueAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.GameNotFound);
    }

    [Fact]
    public async Task EnqueueAsync_WhenGameIsDisabled_ShouldFail()
    {
        // Arrange
        var config = GameMatchConfigFactory.Create(enabled: false);
        var request = EnqueueRequestFactory.Create();
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>()).Returns(config);

        // Act
        var result = await _sut.EnqueueAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.GameDisabled);
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public async Task EnqueueAsync_WhenRuntimeThrows_ShouldFail()
    {
        // Arrange
        var request = EnqueueRequestFactory.Create();
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.EnqueueAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.RedisUnavailable);
    }

    [Fact]
    public async Task EnqueueAsync_WhenTicketStoreFails_ShouldFail()
    {
        // Arrange
        var config = GameMatchConfigFactory.Create(maxQueueDepth: ValidMaxQueueDepth);
        var request = EnqueueRequestFactory.Create();
        _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>()).Returns(config);
        _ticketStore.EnqueueAsync(
                Arg.Any<GameId>(),
                Arg.Any<PlayerId>(),
                Arg.Any<MatchRegion>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure<EnqueueResult, IDomainError>(
                DomainError.TooManyRequests(code: ErrorCodes.QueueFull)));

        // Act
        var result = await _sut.EnqueueAsync(GameIdFactory.Create(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.QueueFull);
    }

    [Fact]
    public async Task GetTicketAsync_WhenTicketExistsForGame_ShouldReturnDto()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued();
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));

        // Act
        var result = await _sut.GetTicketAsync(GameIdFactory.Create(), ticket.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TicketId.Should().Be(ticket.Id.ToString());
        result.Value.PlayerId.Should().Be(DefaultPlayerId);
    }

    [Fact]
    public async Task GetTicketAsync_WhenTicketBelongsToAnotherGame_ShouldFail()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued(gameId: AlternateGameId);
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));

        // Act
        var result = await _sut.GetTicketAsync(GameIdFactory.Create(), ticket.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task GetTicketAsync_WhenTicketStoreFails_ShouldFail()
    {
        // Arrange
        var ticketId = IdFactory.CreateNew<MatchTicket>();
        _ticketStore.GetAsync(ticketId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<MatchTicket, IDomainError>(
                DomainError.NotFound(code: ErrorCodes.TicketNotFound)));

        // Act
        var result = await _sut.GetTicketAsync(GameIdFactory.Create(), ticketId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task CancelAsync_WhenTicketIsQueued_ShouldSucceed()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued();
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));
        _ticketStore.CancelAsync(GameIdFactory.Create(), ticket.Id, Arg.Any<CancellationToken>())
            .Returns(UnitResult.Success<IDomainError>());

        // Act
        var result = await _sut.CancelAsync(GameIdFactory.Create(), ticket.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _ticketStore.Received(1)
            .CancelAsync(GameIdFactory.Create(), ticket.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenTicketIsMatched_ShouldFail()
    {
        // Arrange
        var sessionId = IdFactory.CreateNew<GameSession>();
        var ticket = MatchTicketFactory.RehydrateMatched(sessionId);
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));

        // Act
        var result = await _sut.CancelAsync(GameIdFactory.Create(), ticket.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.AlreadyMatched);
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CancelAsync_WhenTicketIsCancelled_ShouldFail()
    {
        // Arrange
        var ticket = MatchTicketFactory.RehydrateCancelled();
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));

        // Act
        var result = await _sut.CancelAsync(GameIdFactory.Create(), ticket.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.AlreadyCancelled);
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CancelAsync_WhenTicketBelongsToAnotherGame_ShouldFail()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued(gameId: AlternateGameId);
        _ticketStore.GetAsync(ticket.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<MatchTicket, IDomainError>(ticket));

        // Act
        var result = await _sut.CancelAsync(GameIdFactory.Create(), ticket.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.TicketNotFound);
    }
}
