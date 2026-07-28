using FluentAssertions;
using Rovio.Domain.Common;
using Rovio.Domain.Common.Errors;
using Rovio.Matchmaking.Domain.Entities;
using Rovio.Matchmaking.Tests.Data.Factories;
using static Rovio.Matchmaking.Tests.Data.MatchmakingTestData;

namespace Rovio.Matchmaking.Domain.Tests.Unit.Common;

public sealed class IdTests
{
    [Fact]
    public void New_WhenCalled_ShouldCreateNonEmptyId()
    {
        // Act
        var id = Id<MatchTicket>.New();

        // Assert
        id.Value.Should().NotBe(EmptyGuid);
    }

    [Fact]
    public void Create_WhenValidGuidProvided_ShouldCreateId()
    {
        // Act
        var result = Id<MatchTicket>.Create(SampleGuid);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(SampleGuid);
    }

    [Fact]
    public void Create_WhenEmptyGuidProvided_ShouldFail()
    {
        // Act
        var result = Id<MatchTicket>.Create(EmptyGuid);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidId);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(SampleGuidString)]
    [InlineData(IdWithWhitespace)]
    public void Create_WhenValidStringProvided_ShouldCreateId(string value)
    {
        // Act
        var result = Id<MatchTicket>.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(SampleGuid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceString)]
    public void Create_WhenStringIsMissing_ShouldFail(string? value)
    {
        // Act
        var result = Id<MatchTicket>.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidId);
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Theory]
    [InlineData(InvalidIdString)]
    [InlineData(EmptyGuidString)]
    public void Create_WhenStringIsInvalid_ShouldFail(string value)
    {
        // Act
        var result = Id<MatchTicket>.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidId);
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public void FromId_WhenOtherIdProvided_ShouldCopyValue()
    {
        // Arrange
        var source = IdFactory.Create<MatchTicket>();

        // Act
        var result = Id<GameSession>.FromId(source);

        // Assert
        result.Value.Should().Be(source.Value);
    }

    [Fact]
    public void ToString_WhenCalled_ShouldReturnNFormat()
    {
        // Arrange
        var id = IdFactory.Create<MatchTicket>();

        // Act
        var text = id.ToString();

        // Assert
        text.Should().Be(SampleGuid.ToString(GuidNFormat));
    }
}
