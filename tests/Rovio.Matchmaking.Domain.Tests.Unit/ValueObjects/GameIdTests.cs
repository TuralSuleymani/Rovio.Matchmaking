
namespace Rovio.Matchmaking.Domain.Tests.Unit.ValueObjects;

public sealed class GameIdTests
{
    [Theory]
    [InlineData(AngryBirds2GameId)]
    [InlineData(AlternateGameId)]
    public void Create_WhenValidArgumentProvided_ShouldCreateGameId(string value)
    {
        // Act
        var result = GameId.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value.Trim());
    }

    [Fact]
    public void Create_WhenValueHasWhitespace_ShouldTrimValue()
    {
        // Act
        var result = GameId.Create(GameIdWithWhitespace);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(AngryBirds2GameId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceString)]
    public void Create_WhenValueIsMissing_ShouldFail(string? value)
    {
        // Act
        var result = GameId.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidGameId);
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public void Create_WhenValueExceedsMaximumLength_ShouldFail()
    {
        // Act
        var result = GameId.Create(TooLongGameId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidGameId);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ToString_WhenCalled_ShouldReturnValue()
    {
        // Arrange
        var gameId = GameIdFactory.Create();

        // Act
        var text = gameId.ToString();

        // Assert
        text.Should().Be(AngryBirds2GameId);
    }

    [Fact]
    public void Create_WhenValueIsExactMaximumLength_ShouldSucceed()
    {
        // Arrange
        var value = new string('g', GameId.MaximumLength);

        // Act
        var result = GameId.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(GameId.MaximumLength);
    }
}
