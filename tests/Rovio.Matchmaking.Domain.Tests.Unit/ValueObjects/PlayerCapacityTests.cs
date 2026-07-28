
namespace Rovio.Matchmaking.Domain.Tests.Unit.ValueObjects;

public sealed class PlayerCapacityTests
{
    [Theory]
    [InlineData(DefaultMinPlayers, DefaultMaxPlayers)]
    [InlineData(DefaultMinPlayers, DuoMaxPlayers)]
    [InlineData(LargeSquadMinPlayers, LargeSquadMaxPlayers)]
    public void Create_WhenValidArgumentProvided_ShouldCreatePlayerCapacity(
        int minPlayerCount,
        int maxPlayerCount)
    {
        // Act
        var result = PlayerCapacity.Create(minPlayerCount, maxPlayerCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinPlayerCount.Should().Be(minPlayerCount);
        result.Value.MaxPlayerCount.Should().Be(maxPlayerCount);
    }

    [Theory]
    [InlineData(InvalidMinPlayers, DefaultMaxPlayers)]
    [InlineData(ZeroMinPlayers, DefaultMaxPlayers)]
    public void Create_WhenMinimumIsBelowTwo_ShouldFail(int minPlayerCount, int maxPlayerCount)
    {
        // Act
        var result = PlayerCapacity.Create(minPlayerCount, maxPlayerCount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchSize);
    }

    [Fact]
    public void Create_WhenMaximumIsLessThanMinimum_ShouldFail()
    {
        // Act
        var result = PlayerCapacity.Create(
            InvertedCapacityMinPlayers,
            InvertedCapacityMaxPlayers);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchSize);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void CanStart_WhenPlayerCountMeetsMinimum_ShouldReturnTrue()
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var canStart = capacity.CanStart(DefaultMinPlayers);

        // Assert
        canStart.Should().BeTrue();
    }

    [Fact]
    public void CanStart_WhenPlayerCountIsBelowMinimum_ShouldReturnFalse()
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var canStart = capacity.CanStart(BelowMinimumPlayerCount);

        // Assert
        canStart.Should().BeFalse();
    }

    [Fact]
    public void IsFull_WhenPlayerCountMeetsMaximum_ShouldReturnTrue()
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var isFull = capacity.IsFull(DefaultMaxPlayers);

        // Assert
        isFull.Should().BeTrue();
    }

    [Fact]
    public void CanAccept_WhenPlayerCountIsBelowMaximum_ShouldReturnTrue()
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var canAccept = capacity.CanAccept(MidCapacityPlayerCount);

        // Assert
        canAccept.Should().BeTrue();
    }

    [Fact]
    public void CanAccept_WhenPlayerCountIsAtMaximum_ShouldReturnFalse()
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var canAccept = capacity.CanAccept(DefaultMaxPlayers);

        // Assert
        canAccept.Should().BeFalse();
    }

    [Theory]
    [InlineData(DefaultMinPlayers, true)]
    [InlineData(DefaultMaxPlayers, true)]
    [InlineData(BelowMinimumPlayerCount, false)]
    [InlineData(AboveMaximumPlayerCount, false)]
    public void Contains_WhenPlayerCountIsEvaluated_ShouldReturnExpectedResult(
        int playerCount,
        bool expected)
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var contains = capacity.Contains(playerCount);

        // Assert
        contains.Should().Be(expected);
    }

    [Fact]
    public void IsFull_WhenPlayerCountIsBelowMaximum_ShouldReturnFalse()
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var isFull = capacity.IsFull(MidCapacityPlayerCount);

        // Assert
        isFull.Should().BeFalse();
    }

    [Fact]
    public void CanStart_WhenPlayerCountExceedsMaximum_ShouldStillReturnTrue()
    {
        // Arrange
        var capacity = PlayerCapacityFactory.Create();

        // Act
        var canStart = capacity.CanStart(AboveMaximumPlayerCount);

        // Assert
        canStart.Should().BeTrue();
    }

    [Fact]
    public void Create_WhenMinimumIsNegative_ShouldFail()
    {
        // Act
        var result = PlayerCapacity.Create(-1, DefaultMaxPlayers);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchSize);
    }
}
