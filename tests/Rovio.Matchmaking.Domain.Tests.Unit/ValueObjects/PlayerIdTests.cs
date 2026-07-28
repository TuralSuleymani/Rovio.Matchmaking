
namespace Rovio.Matchmaking.Domain.Tests.Unit.ValueObjects;

public sealed class PlayerIdTests
{
    [Theory]
    [InlineData(DefaultPlayerId)]
    [InlineData(SecondPlayerId)]
    public void Create_WhenValidArgumentProvided_ShouldCreatePlayerId(string value)
    {
        // Act
        var result = PlayerId.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value.Trim());
    }

    [Fact]
    public void Create_WhenValueHasWhitespace_ShouldTrimValue()
    {
        // Act
        var result = PlayerId.Create(PlayerIdWithWhitespace);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(TrimmedPlayerId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceString)]
    public void Create_WhenValueIsMissing_ShouldFail(string? value)
    {
        // Act
        var result = PlayerId.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidPlayerId);
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public void Create_WhenValueExceedsMaximumLength_ShouldFail()
    {
        // Arrange
        var tooLongValue = new string('p', PlayerId.MaximumLength + 1);

        // Act
        var result = PlayerId.Create(tooLongValue);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidPlayerId);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ToString_WhenCalled_ShouldReturnValue()
    {
        // Arrange
        var playerId = PlayerIdFactory.Create();

        // Act
        var text = playerId.ToString();

        // Assert
        text.Should().Be(DefaultPlayerId);
    }

    [Fact]
    public void Create_WhenValueIsExactMaximumLength_ShouldSucceed()
    {
        // Arrange
        var value = new string('p', PlayerId.MaximumLength);

        // Act
        var result = PlayerId.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(PlayerId.MaximumLength);
    }
}
