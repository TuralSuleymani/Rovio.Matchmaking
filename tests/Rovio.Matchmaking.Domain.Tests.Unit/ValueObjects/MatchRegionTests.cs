
namespace Rovio.Matchmaking.Domain.Tests.Unit.ValueObjects;

public sealed class MatchRegionTests
{
    [Theory]
    [InlineData(DefaultRegion)]
    [InlineData(NaRegion)]
    public void Create_WhenValidArgumentProvided_ShouldCreateMatchRegion(string value)
    {
        // Act
        var result = MatchRegion.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value.Trim().ToLowerInvariant());
    }

    [Fact]
    public void Create_WhenValueHasWhitespaceAndMixedCase_ShouldNormalizeValue()
    {
        // Act
        var result = MatchRegion.Create(RegionWithWhitespace);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(NormalizedRegion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceString)]
    public void Create_WhenValueIsMissing_ShouldFail(string? value)
    {
        // Act
        var result = MatchRegion.Create(value!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchRegion);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_WhenValueExceedsMaximumLength_ShouldFail()
    {
        // Act
        var result = MatchRegion.Create(TooLongRegion);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchRegion);
    }

    [Fact]
    public void IsSameAs_WhenRegionsAreEqual_ShouldReturnTrue()
    {
        // Arrange
        var first = MatchRegionFactory.Create();
        var second = MatchRegionFactory.Create(RegionWithWhitespace);

        // Act
        var areSame = first.IsSameAs(second);

        // Assert
        areSame.Should().BeTrue();
    }

    [Fact]
    public void IsSameAs_WhenRegionsDiffer_ShouldReturnFalse()
    {
        // Arrange
        var first = MatchRegionFactory.Create(DefaultRegion);
        var second = MatchRegionFactory.Create(NaRegion);

        // Act
        var areSame = first.IsSameAs(second);

        // Assert
        areSame.Should().BeFalse();
    }

    [Fact]
    public void Create_WhenValueIsExactMaximumLength_ShouldSucceed()
    {
        // Arrange
        var value = new string('r', MatchRegion.MaximumLength);

        // Act
        var result = MatchRegion.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(MatchRegion.MaximumLength);
    }

    [Fact]
    public void ToString_WhenCalled_ShouldReturnValue()
    {
        // Arrange
        var region = MatchRegionFactory.Create();

        // Act
        var text = region.ToString();

        // Assert
        text.Should().Be(DefaultRegion);
    }
}
