
namespace Rovio.Matchmaking.Domain.Tests.Unit.ValueObjects;

public sealed class LatencyDeltaTests
{
    [Theory]
    [InlineData(ZeroLatencyMs)]
    [InlineData(DefaultLatencyDeltaMs)]
    [InlineData(LargeLatencyDeltaMs)]
    public void Create_WhenValidArgumentProvided_ShouldCreateLatencyDelta(int milliseconds)
    {
        // Act
        var result = LatencyDelta.Create(milliseconds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milliseconds.Should().Be(milliseconds);
    }

    [Fact]
    public void Create_WhenValueIsNegative_ShouldFail()
    {
        // Act
        var result = LatencyDelta.Create(NegativeLatencyDeltaMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyDelta);
    }

    [Fact]
    public void Allows_WhenActualDeltaIsWithinLimit_ShouldReturnTrue()
    {
        // Arrange
        var limit = LatencyDeltaFactory.Create(LargeLatencyDeltaMs);
        var actual = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);

        // Act
        var allows = limit.Allows(actual);

        // Assert
        allows.Should().BeTrue();
    }

    [Fact]
    public void Allows_WhenActualDeltaExceedsLimit_ShouldReturnFalse()
    {
        // Arrange
        var limit = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);
        var actual = LatencyDeltaFactory.Create(LargeLatencyDeltaMs);

        // Act
        var allows = limit.Allows(actual);

        // Assert
        allows.Should().BeFalse();
    }

    [Fact]
    public void Add_WhenSumIsWithinRange_ShouldReturnCombinedDelta()
    {
        // Arrange
        var first = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);
        var second = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);

        // Act
        var result = first.Add(second);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milliseconds.Should().Be(ExpectedCombinedLatencyDeltaMs);
    }

    [Fact]
    public void Add_WhenSumOverflows_ShouldFail()
    {
        // Arrange
        var first = LatencyDelta.Create(int.MaxValue).Value;
        var second = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);

        // Act
        var result = first.Add(second);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.LatencyDeltaOverflow);
    }

    [Fact]
    public void MultiplyBy_WhenMultiplierIsValid_ShouldReturnScaledDelta()
    {
        // Arrange
        var delta = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);

        // Act
        var result = delta.MultiplyBy(LatencyDeltaMultiplier);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milliseconds.Should().Be(ExpectedScaledLatencyDeltaMs);
    }

    [Fact]
    public void MultiplyBy_WhenMultiplierIsNegative_ShouldFail()
    {
        // Arrange
        var delta = LatencyDeltaFactory.Create();

        // Act
        var result = delta.MultiplyBy(NegativeLatencyDeltaMultiplier);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyDeltaMultiplier);
    }

    [Fact]
    public void MultiplyBy_WhenProductOverflows_ShouldFail()
    {
        // Arrange
        var delta = LatencyDelta.Create(int.MaxValue).Value;

        // Act
        var result = delta.MultiplyBy(OverflowLatencyDeltaMultiplier);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.LatencyDeltaOverflow);
    }

    [Fact]
    public void Min_WhenFirstIsSmaller_ShouldReturnFirst()
    {
        // Arrange
        var first = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);
        var second = LatencyDeltaFactory.Create(LargeLatencyDeltaMs);

        // Act
        var min = LatencyDelta.Min(first, second);

        // Assert
        min.Should().Be(first);
    }

    [Fact]
    public void Min_WhenSecondIsSmaller_ShouldReturnSecond()
    {
        // Arrange
        var first = LatencyDeltaFactory.Create(LargeLatencyDeltaMs);
        var second = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);

        // Act
        var min = LatencyDelta.Min(first, second);

        // Assert
        min.Should().Be(second);
    }

    [Fact]
    public void Allows_WhenActualDeltaEqualsLimit_ShouldReturnTrue()
    {
        // Arrange
        var limit = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);
        var actual = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);

        // Act
        var allows = limit.Allows(actual);

        // Assert
        allows.Should().BeTrue();
    }

    [Fact]
    public void Min_WhenValuesAreEqual_ShouldReturnEither()
    {
        // Arrange
        var first = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);
        var second = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);

        // Act
        var min = LatencyDelta.Min(first, second);

        // Assert
        min.Milliseconds.Should().Be(DefaultLatencyDeltaMs);
    }

    [Fact]
    public void MultiplyBy_WhenMultiplierIsZero_ShouldReturnZero()
    {
        // Arrange
        var delta = LatencyDeltaFactory.Create(SmallLatencyDeltaMs);

        // Act
        var result = delta.MultiplyBy(0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milliseconds.Should().Be(0);
    }

    [Fact]
    public void ToTimeSpan_WhenCalled_ShouldMatchMilliseconds()
    {
        // Arrange
        var delta = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);

        // Act
        var timeSpan = delta.ToTimeSpan();

        // Assert
        timeSpan.Should().Be(TimeSpan.FromMilliseconds(DefaultLatencyDeltaMs));
    }

    [Fact]
    public void ToString_WhenCalled_ShouldIncludeMilliseconds()
    {
        // Arrange
        var delta = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);

        // Act
        var text = delta.ToString();

        // Assert
        text.Should().Be($"{DefaultLatencyDeltaMs} ms");
    }
}
