
namespace Rovio.Matchmaking.Domain.Tests.Unit.ValueObjects;

public sealed class LatencyTests
{
    [Theory]
    [InlineData(ZeroLatencyMs)]
    [InlineData(DefaultLatencyMs)]
    [InlineData(IncompatibleLatencyMs)]
    public void Create_WhenValidArgumentProvided_ShouldCreateLatency(int milliseconds)
    {
        // Act
        var result = Latency.Create(milliseconds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milliseconds.Should().Be(milliseconds);
    }

    [Fact]
    public void Create_WhenValueIsNegative_ShouldFail()
    {
        // Act
        var result = Latency.Create(NegativeLatencyMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatency);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void DifferenceFrom_WhenLatenciesDiffer_ShouldReturnAbsoluteDelta()
    {
        // Arrange
        var first = LatencyFactory.Create(DefaultLatencyMs);
        var second = LatencyFactory.Create(IncompatibleLatencyMs);

        // Act
        var delta = first.DifferenceFrom(second);

        // Assert
        delta.Milliseconds.Should().Be(ExpectedLatencyDifferenceMs);
    }

    [Fact]
    public void IsWithin_WhenLatencyIsBelowOrEqualMaximum_ShouldReturnTrue()
    {
        // Arrange
        var latency = LatencyFactory.Create(DefaultLatencyMs);
        var maximum = LatencyFactory.Create(CompatibleLatencyMs);

        // Act
        var isWithin = latency.IsWithin(maximum);

        // Assert
        isWithin.Should().BeTrue();
    }

    [Fact]
    public void IsWithin_WhenLatencyExceedsMaximum_ShouldReturnFalse()
    {
        // Arrange
        var latency = LatencyFactory.Create(IncompatibleLatencyMs);
        var maximum = LatencyFactory.Create(DefaultLatencyMs);

        // Act
        var isWithin = latency.IsWithin(maximum);

        // Assert
        isWithin.Should().BeFalse();
    }

    [Fact]
    public void ToTimeSpan_WhenCalled_ShouldMatchMilliseconds()
    {
        // Arrange
        var latency = LatencyFactory.Create(DefaultLatencyMs);

        // Act
        var timeSpan = latency.ToTimeSpan();

        // Assert
        timeSpan.Should().Be(TimeSpan.FromMilliseconds(DefaultLatencyMs));
    }

    [Fact]
    public void DifferenceFrom_WhenLatenciesAreEqual_ShouldReturnZero()
    {
        // Arrange
        var first = LatencyFactory.Create(DefaultLatencyMs);
        var second = LatencyFactory.Create(DefaultLatencyMs);

        // Act
        var delta = first.DifferenceFrom(second);

        // Assert
        delta.Milliseconds.Should().Be(0);
    }

    [Fact]
    public void DifferenceFrom_WhenOrderIsReversed_ShouldReturnSameAbsoluteDelta()
    {
        // Arrange
        var first = LatencyFactory.Create(DefaultLatencyMs);
        var second = LatencyFactory.Create(IncompatibleLatencyMs);

        // Act
        var forward = first.DifferenceFrom(second);
        var reverse = second.DifferenceFrom(first);

        // Assert
        forward.Should().Be(reverse);
    }

    [Fact]
    public void IsWithin_WhenLatencyEqualsMaximum_ShouldReturnTrue()
    {
        // Arrange
        var latency = LatencyFactory.Create(DefaultLatencyMs);
        var maximum = LatencyFactory.Create(DefaultLatencyMs);

        // Act
        var isWithin = latency.IsWithin(maximum);

        // Assert
        isWithin.Should().BeTrue();
    }

    [Fact]
    public void ToString_WhenCalled_ShouldIncludeMilliseconds()
    {
        // Arrange
        var latency = LatencyFactory.Create(DefaultLatencyMs);

        // Act
        var text = latency.ToString();

        // Assert
        text.Should().Be($"{DefaultLatencyMs} ms");
    }
}
