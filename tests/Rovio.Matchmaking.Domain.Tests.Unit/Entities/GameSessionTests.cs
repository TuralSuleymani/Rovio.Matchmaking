
namespace Rovio.Matchmaking.Domain.Tests.Unit.Entities;

public sealed class GameSessionTests
{
    [Fact]
    public void Create_WhenValidArgumentProvided_ShouldCreateFormedSession()
    {
        // Arrange
        var playerIds = new[]
        {
            PlayerIdFactory.Create(),
            PlayerIdFactory.CreateSecond()
        };

        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.Create(),
            allowLateJoin: true,
            playerIds,
            DefaultNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SessionStatus.Formed);
        result.Value.AllowLateJoin.Should().BeTrue();
        result.Value.CanLateJoin.Should().BeTrue();
        result.Value.OpenSlots.Should().Be(DefaultMaxPlayers - playerIds.Length);
        result.Value.PlayerIds.Should().HaveCount(playerIds.Length);
        result.Value.StartedAt.Should().Be(DefaultNow);
    }

    [Fact]
    public void Create_WhenPlayerCountReachesMaximum_ShouldCreateFullSessionAndPreserveLateJoinPolicy()
    {
        // Act
        var session = GameSessionFactory.CreateFull(allowLateJoin: true);

        // Assert
        session.Status.Should().Be(SessionStatus.Full);
        session.AllowLateJoin.Should().BeTrue();
        session.CanLateJoin.Should().BeFalse();
        session.OpenSlots.Should().Be(0);
    }

    [Fact]
    public void Create_WhenPlayerCountIsBelowMinimum_ShouldFail()
    {
        // Arrange
        var playerIds = new[] { PlayerIdFactory.Create() };

        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.Create(),
            allowLateJoin: true,
            playerIds,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InsufficientPlayers);
    }

    [Fact]
    public void Create_WhenPlayerCountExceedsMaximum_ShouldFail()
    {
        // Arrange
        var playerIds = new[]
        {
            PlayerIdFactory.Create(),
            PlayerIdFactory.CreateSecond(),
            PlayerIdFactory.CreateThird()
        };

        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.CreateDuo(),
            allowLateJoin: true,
            playerIds,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.PlayerCapacityExceeded);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_WhenDuplicatePlayersProvided_ShouldFail()
    {
        // Arrange
        var player = PlayerIdFactory.Create();
        var playerIds = new[] { player, player };

        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.CreateDuo(),
            allowLateJoin: true,
            playerIds,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.DuplicateSessionPlayer);
    }

    [Fact]
    public void EnsureCanLateJoin_WhenLateJoinAllowedAndSlotOpen_ShouldSucceed()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot(allowLateJoin: true);

        // Act
        var result = session.EnsureCanLateJoin();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EnsureCanLateJoin_WhenLateJoinDisabled_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot(allowLateJoin: false);

        // Act
        var result = session.EnsureCanLateJoin();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.LateJoinDisabled);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void EnsureCanLateJoin_WhenSessionIsFull_ShouldFail()
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

        // Act
        var result = session.EnsureCanLateJoin();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.SessionFull);
    }

    [Fact]
    public void TryAddPlayer_WhenSlotOpenAndStillUnderMaximum_ShouldKeepFormedStatus()
    {
        // Arrange
        var session = GameSessionFactory.Create(
            playerCapacity: PlayerCapacityFactory.Create(),
            playerIds:
            [
                PlayerIdFactory.Create(),
                PlayerIdFactory.CreateSecond()
            ]);
        var joiner = PlayerIdFactory.CreateThird();

        // Act
        var result = session.TryAddPlayer(joiner);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.PlayerIds.Should().Contain(joiner);
        session.Status.Should().Be(SessionStatus.Formed);
        session.CanLateJoin.Should().BeTrue();
        session.OpenSlots.Should().Be(1);
    }

    [Fact]
    public void TryAddPlayer_WhenFillingLastSlot_ShouldBecomeFull()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot();
        var joiner = PlayerIdFactory.CreateThird();

        // Act
        var result = session.TryAddPlayer(joiner);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.PlayerIds.Should().Contain(joiner);
        session.Status.Should().Be(SessionStatus.Full);
        session.CanLateJoin.Should().BeFalse();
    }

    [Fact]
    public void TryAddPlayer_WhenDuplicatePlayer_ShouldFail()
    {
        // Arrange
        var existing = PlayerIdFactory.Create();
        var session = GameSessionFactory.Create(
            playerCapacity: PlayerCapacityFactory.CreateTrio(),
            playerIds: [existing, PlayerIdFactory.CreateSecond()]);

        // Act
        var result = session.TryAddPlayer(existing);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.DuplicateSessionPlayer);
    }

    [Fact]
    public void TryAddPlayer_WhenLateJoinDisabled_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot(allowLateJoin: false);

        // Act
        var result = session.TryAddPlayer(PlayerIdFactory.CreateThird());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.LateJoinDisabled);
    }

    [Fact]
    public void TryAddPlayer_WhenSessionIsFull_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.CreateFull(allowLateJoin: true);

        // Act
        var result = session.TryAddPlayer(PlayerIdFactory.CreateThird());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.SessionFull);
    }

    [Fact]
    public void CanLateJoin_WhenLateJoinDisabledWithOpenSlots_ShouldBeFalse()
    {
        // Arrange
        var session = GameSessionFactory.CreateWithOpenSlot(allowLateJoin: false);

        // Assert
        session.AllowLateJoin.Should().BeFalse();
        session.CanLateJoin.Should().BeFalse();
        session.OpenSlots.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EnsureCanLateJoin_WhenOpenSlotsAreZeroButStatusIsFormed_ShouldFail()
    {
        // Arrange
        var session = GameSessionFactory.Rehydrate(
            SessionStatus.Formed,
            allowLateJoin: true,
            playerCapacity: PlayerCapacityFactory.CreateDuo(),
            playerIds:
            [
                PlayerIdFactory.Create(),
                PlayerIdFactory.CreateSecond()
            ]);

        // Act
        var result = session.EnsureCanLateJoin();

        // Assert
        session.OpenSlots.Should().Be(0);
        session.CanLateJoin.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.SessionFull);
    }

    [Fact]
    public void Create_WhenRegionIsNull_ShouldFail()
    {
        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            null!,
            PlayerCapacityFactory.Create(),
            allowLateJoin: true,
            [PlayerIdFactory.Create(), PlayerIdFactory.CreateSecond()],
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidSession);
    }

    [Fact]
    public void Create_WhenPlayerCapacityIsNull_ShouldFail()
    {
        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            null!,
            allowLateJoin: true,
            [PlayerIdFactory.Create(), PlayerIdFactory.CreateSecond()],
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidSession);
    }

    [Fact]
    public void Create_WhenPlayerIdsAreNull_ShouldFail()
    {
        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.Create(),
            allowLateJoin: true,
            null!,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidSession);
    }

    [Fact]
    public void Create_WhenPlayerCountEqualsMinimum_ShouldSucceed()
    {
        // Arrange
        var playerIds = new[]
        {
            PlayerIdFactory.Create(),
            PlayerIdFactory.CreateSecond()
        };

        // Act
        var result = GameSession.Create(
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.Create(),
            allowLateJoin: true,
            playerIds,
            DefaultNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlayerIds.Should().HaveCount(DefaultMinPlayers);
    }

    [Fact]
    public void Rehydrate_WhenPlayersAreNull_ShouldFail()
    {
        // Act
        var result = GameSession.Rehydrate(
            Rovio.Domain.Common.Id<GameSession>.New(),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.Create(),
            SessionStatus.Formed,
            allowLateJoin: true,
            null!,
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidSession);
    }

    [Fact]
    public void Rehydrate_WhenDuplicatePlayersProvided_ShouldFail()
    {
        // Arrange
        var player = PlayerIdFactory.Create();

        // Act
        var result = GameSession.Rehydrate(
            Rovio.Domain.Common.Id<GameSession>.New(),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.CreateDuo(),
            SessionStatus.Formed,
            allowLateJoin: true,
            [player, player],
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.DuplicateSessionPlayer);
    }
}
