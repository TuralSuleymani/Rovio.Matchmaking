
namespace Rovio.Matchmaking.Domain.Tests.Unit.Entities;

public sealed class GameMatchConfigTests
{
    [Fact]
    public void Create_WhenValidArgumentProvided_ShouldCreateConfig()
    {
        // Act
        var result = GameMatchConfig.Create(
            AngryBirds2GameId,
            DefaultMinPlayers,
            DefaultMaxPlayers,
            allowLateJoin: true,
            enabled: true,
            ValidMaxQueueDepth,
            LatencyPolicyFactory.CreateDefault(),
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Value.Should().Be(AngryBirds2GameId);
        result.Value.PlayerCapacity.MinPlayerCount.Should().Be(DefaultMinPlayers);
        result.Value.PlayerCapacity.MaxPlayerCount.Should().Be(DefaultMaxPlayers);
        result.Value.AllowLateJoin.Should().BeTrue();
        result.Value.Enabled.Should().BeTrue();
        result.Value.MaxQueueDepth.Should().Be(ValidMaxQueueDepth);
    }

    [Fact]
    public void Create_WhenGameIdIsInvalid_ShouldFail()
    {
        // Act
        var result = GameMatchConfig.Create(
            string.Empty,
            DefaultMinPlayers,
            DefaultMaxPlayers,
            allowLateJoin: true,
            enabled: true,
            null,
            LatencyPolicyFactory.CreateDefault(),
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidGameId);
    }

    [Fact]
    public void Create_WhenPlayerCapacityIsInvalid_ShouldFail()
    {
        // Act
        var result = GameMatchConfig.Create(
            AngryBirds2GameId,
            DefaultMaxPlayers,
            DefaultMinPlayers,
            allowLateJoin: false,
            enabled: true,
            null,
            LatencyPolicyFactory.CreateDefault(),
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchSize);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(InvalidMaxQueueDepth)]
    [InlineData(NegativeMaxQueueDepth)]
    public void Create_WhenMaxQueueDepthIsInvalid_ShouldFail(int maxQueueDepth)
    {
        // Act
        var result = GameMatchConfig.Create(
            AngryBirds2GameId,
            DefaultMinPlayers,
            DefaultMaxPlayers,
            allowLateJoin: true,
            enabled: true,
            maxQueueDepth,
            LatencyPolicyFactory.CreateDefault(),
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidConfig);
    }

    [Fact]
    public void Create_WhenMaxQueueDepthIsNull_ShouldSucceed()
    {
        // Act
        var config = GameMatchConfigFactory.Create(maxQueueDepth: null);

        // Assert
        config.MaxQueueDepth.Should().BeNull();
    }

    [Fact]
    public void CreateAngryBirds2Defaults_WhenCalled_ShouldUseDefaultPolicyAndCapacity()
    {
        // Act
        var config = GameMatchConfigFactory.CreateAngryBirds2Defaults();

        // Assert
        config.GameId.Value.Should().Be(AngryBirds2GameId);
        config.PlayerCapacity.MinPlayerCount.Should().Be(DefaultMinPlayers);
        config.PlayerCapacity.MaxPlayerCount.Should().Be(DefaultMaxPlayers);
        config.AllowLateJoin.Should().BeTrue();
        config.Enabled.Should().BeTrue();
        config.MaxQueueDepth.Should().BeNull();
    }

    [Fact]
    public void Create_WhenSameGameIdUsedTwice_ShouldProduceSameEntityId()
    {
        // Act
        var first = GameMatchConfigFactory.Create();
        var second = GameMatchConfigFactory.Create();

        // Assert
        first.Id.Should().Be(second.Id);
    }

    [Fact]
    public void Create_WhenStronglyTypedGameIdProvided_ShouldCreateConfig()
    {
        // Arrange
        var gameId = GameIdFactory.Create(AlternateGameId);

        // Act
        var result = GameMatchConfig.Create(
            gameId,
            DefaultMinPlayers,
            DefaultMaxPlayers,
            allowLateJoin: false,
            enabled: false,
            ValidMaxQueueDepth,
            LatencyPolicyFactory.CreateDefault(),
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(gameId);
        result.Value.AllowLateJoin.Should().BeFalse();
        result.Value.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Create_WhenMaxQueueDepthIsOne_ShouldSucceed()
    {
        // Act
        var result = GameMatchConfig.Create(
            AngryBirds2GameId,
            DefaultMinPlayers,
            DefaultMaxPlayers,
            allowLateJoin: true,
            enabled: true,
            maxQueueDepth: 1,
            LatencyPolicyFactory.CreateDefault(),
            DefaultNow,
            DefaultNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxQueueDepth.Should().Be(1);
    }

    [Fact]
    public void CreateAngryBirds2Defaults_WhenCalled_ShouldUseDefaultLatencyPolicy()
    {
        // Act
        var config = GameMatchConfig.CreateAngryBirds2Defaults(DefaultNow);

        // Assert
        config.LatencyPolicy.Should().Be(LatencyPolicy.Default);
    }
}
