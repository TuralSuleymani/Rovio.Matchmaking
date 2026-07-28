
namespace Rovio.Matchmaking.Domain.Tests.Unit.Entities;

public sealed class MatchTicketTests
{
    [Fact]
    public void CreateQueued_WhenValidArgumentProvided_ShouldCreateQueuedTicket()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            DefaultPlayerId,
            AngryBirds2GameId,
            DefaultRegion,
            DefaultLatencyMs,
            OlderEnqueueAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlayerId.Value.Should().Be(DefaultPlayerId);
        result.Value.GameId.Value.Should().Be(AngryBirds2GameId);
        result.Value.Region.Value.Should().Be(DefaultRegion);
        result.Value.Latency.Milliseconds.Should().Be(DefaultLatencyMs);
        result.Value.Status.Should().Be(TicketStatus.Queued);
        result.Value.SessionId.Should().BeNull();
    }

    [Fact]
    public void CreateQueued_WhenPlayerIdIsInvalid_ShouldFail()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            string.Empty,
            AngryBirds2GameId,
            DefaultRegion,
            DefaultLatencyMs,
            OlderEnqueueAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidPlayerId);
    }

    [Fact]
    public void CreateQueued_WhenGameIdIsInvalid_ShouldFail()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            DefaultPlayerId,
            string.Empty,
            DefaultRegion,
            DefaultLatencyMs,
            OlderEnqueueAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidGameId);
    }

    [Fact]
    public void CreateQueued_WhenRegionIsInvalid_ShouldFail()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            DefaultPlayerId,
            AngryBirds2GameId,
            string.Empty,
            DefaultLatencyMs,
            OlderEnqueueAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchRegion);
    }

    [Fact]
    public void CreateQueued_WhenLatencyIsInvalid_ShouldFail()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            DefaultPlayerId,
            AngryBirds2GameId,
            DefaultRegion,
            NegativeLatencyMs,
            OlderEnqueueAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatency);
    }

    [Fact]
    public void CreateQueued_WhenStronglyTypedArgumentsProvided_ShouldCreateQueuedTicket()
    {
        // Arrange
        var playerId = PlayerIdFactory.Create();
        var gameId = GameIdFactory.Create();
        var region = MatchRegionFactory.Create();
        var latency = LatencyFactory.Create();

        // Act
        var result = MatchTicket.CreateQueued(
            playerId,
            gameId,
            region,
            latency,
            OlderEnqueueAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TicketStatus.Queued);
    }

    [Fact]
    public void WaitTime_WhenNowIsAfterEnqueue_ShouldReturnElapsedDuration()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued(enqueuedAt: OlderEnqueueAt);
        var expected = DefaultNow - OlderEnqueueAt;

        // Act
        var waitTime = ticket.WaitTime(DefaultNow);

        // Assert
        waitTime.Should().Be(expected);
    }

    [Fact]
    public void WaitTime_WhenNowIsBeforeEnqueue_ShouldReturnZero()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued(enqueuedAt: DefaultNow);
        var earlier = DefaultNow.AddMinutes(-1);

        // Act
        var waitTime = ticket.WaitTime(earlier);

        // Assert
        waitTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Rehydrate_WhenMatchedStatusProvided_ShouldPreserveState()
    {
        // Arrange
        var sessionId = Rovio.Domain.Common.Id<GameSession>.New();

        // Act
        var ticket = MatchTicketFactory.RehydrateMatched(sessionId);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Matched);
        ticket.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void Rehydrate_WhenMatchedWithoutSession_ShouldFail()
    {
        // Act
        var result = MatchTicket.Rehydrate(
            Rovio.Domain.Common.Id<MatchTicket>.New(),
            PlayerIdFactory.Create(),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            OlderEnqueueAt,
            TicketStatus.Matched,
            sessionId: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidTicketState);
    }

    [Fact]
    public void MarkMatched_WhenQueued_ShouldTransitionToMatched()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued();
        var sessionId = Rovio.Domain.Common.Id<GameSession>.New();

        // Act
        var result = ticket.MarkMatched(sessionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.Matched);
        ticket.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void Cancel_WhenMatched_ShouldFail()
    {
        // Arrange
        var ticket = MatchTicketFactory.RehydrateMatched(Rovio.Domain.Common.Id<GameSession>.New());

        // Act
        var result = ticket.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidTicketTransition);
    }

    [Fact]
    public void WaitTime_WhenNowEqualsEnqueue_ShouldReturnZero()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued(enqueuedAt: DefaultNow);

        // Act
        var waitTime = ticket.WaitTime(DefaultNow);

        // Assert
        waitTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CreateQueued_WhenTypedPlayerIdIsNull_ShouldFail()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            null!,
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            OlderEnqueueAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidPlayerId);
    }

    [Fact]
    public void CreateQueued_WhenTypedGameIdIsNull_ShouldFail()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            PlayerIdFactory.Create(),
            null!,
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            OlderEnqueueAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidGameId);
    }

    [Fact]
    public void CreateQueued_WhenTypedRegionIsNull_ShouldFail()
    {
        // Act
        var result = MatchTicket.CreateQueued(
            PlayerIdFactory.Create(),
            GameIdFactory.Create(),
            null!,
            LatencyFactory.Create(),
            OlderEnqueueAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidRegion);
    }

    [Fact]
    public void Rehydrate_WhenQueuedWithSession_ShouldFail()
    {
        // Act
        var result = MatchTicket.Rehydrate(
            Rovio.Domain.Common.Id<MatchTicket>.New(),
            PlayerIdFactory.Create(),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            OlderEnqueueAt,
            TicketStatus.Queued,
            Rovio.Domain.Common.Id<GameSession>.New());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidTicketState);
    }

    [Fact]
    public void Rehydrate_WhenCancelledWithSession_ShouldFail()
    {
        // Act
        var result = MatchTicket.Rehydrate(
            Rovio.Domain.Common.Id<MatchTicket>.New(),
            PlayerIdFactory.Create(),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            OlderEnqueueAt,
            TicketStatus.Cancelled,
            Rovio.Domain.Common.Id<GameSession>.New());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidTicketState);
    }

    [Fact]
    public void Rehydrate_WhenQueuedWithoutSession_ShouldSucceed()
    {
        // Act
        var result = MatchTicket.Rehydrate(
            Rovio.Domain.Common.Id<MatchTicket>.New(),
            PlayerIdFactory.Create(),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            OlderEnqueueAt,
            TicketStatus.Queued,
            sessionId: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TicketStatus.Queued);
        result.Value.SessionId.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_WhenCancelledWithoutSession_ShouldSucceed()
    {
        // Act
        var ticket = MatchTicketFactory.RehydrateCancelled();

        // Assert
        ticket.Status.Should().Be(TicketStatus.Cancelled);
        ticket.SessionId.Should().BeNull();
    }

    [Fact]
    public void MarkMatched_WhenAlreadyMatched_ShouldFail()
    {
        // Arrange
        var ticket = MatchTicketFactory.RehydrateMatched(Rovio.Domain.Common.Id<GameSession>.New());

        // Act
        var result = ticket.MarkMatched(Rovio.Domain.Common.Id<GameSession>.New());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidTicketTransition);
    }

    [Fact]
    public void MarkMatched_WhenCancelled_ShouldFail()
    {
        // Arrange
        var ticket = MatchTicketFactory.RehydrateCancelled();

        // Act
        var result = ticket.MarkMatched(Rovio.Domain.Common.Id<GameSession>.New());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidTicketTransition);
    }

    [Fact]
    public void Cancel_WhenQueued_ShouldTransitionToCancelled()
    {
        // Arrange
        var ticket = MatchTicketFactory.CreateQueued();

        // Act
        var result = ticket.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.Cancelled);
        ticket.SessionId.Should().BeNull();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldFail()
    {
        // Arrange
        var ticket = MatchTicketFactory.RehydrateCancelled();

        // Act
        var result = ticket.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidTicketTransition);
    }
}
