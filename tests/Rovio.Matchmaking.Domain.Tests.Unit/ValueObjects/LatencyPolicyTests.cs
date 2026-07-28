
namespace Rovio.Matchmaking.Domain.Tests.Unit.ValueObjects;

public sealed class LatencyPolicyTests
{
    [Fact]
    public void Create_WhenValidArgumentProvided_ShouldCreateLatencyPolicy()
    {
        // Act
        var result = LatencyPolicy.Create(
            DefaultLatencyDeltaMs,
            DefaultExpansionIntervalSeconds,
            DefaultExpansionStepMs,
            AbsoluteMaxLatencyDeltaMs);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BaseMaximumDelta.Milliseconds.Should().Be(DefaultLatencyDeltaMs);
        result.Value.ExpansionInterval.Should().Be(TimeSpan.FromSeconds(DefaultExpansionIntervalSeconds));
        result.Value.ExpansionStep.Milliseconds.Should().Be(DefaultExpansionStepMs);
        result.Value.AbsoluteMaximumDelta.Milliseconds.Should().Be(AbsoluteMaxLatencyDeltaMs);
    }

    [Theory]
    [InlineData(ZeroExpansionIntervalSeconds)]
    [InlineData(NegativeExpansionIntervalSeconds)]
    public void Create_WhenExpansionIntervalIsInvalid_ShouldFail(int expansionIntervalSeconds)
    {
        // Act
        var result = LatencyPolicy.Create(
            DefaultLatencyDeltaMs,
            expansionIntervalSeconds,
            DefaultExpansionStepMs,
            AbsoluteMaxLatencyDeltaMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyPolicy);
    }

    [Fact]
    public void Create_WhenAbsoluteMaximumIsBelowBase_ShouldFail()
    {
        // Act
        var result = LatencyPolicy.Create(
            BaseMaxAboveAbsoluteBaseMs,
            DefaultExpansionIntervalSeconds,
            DefaultExpansionStepMs,
            BaseMaxAboveAbsoluteAbsoluteMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyPolicy);
    }

    [Fact]
    public void Create_WhenNestedLatencyDeltaIsInvalid_ShouldFail()
    {
        // Act
        var result = LatencyPolicy.Create(
            NegativeLatencyDeltaMs,
            DefaultExpansionIntervalSeconds,
            DefaultExpansionStepMs,
            AbsoluteMaxLatencyDeltaMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyDelta);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenWaitIsZero_ShouldReturnBaseMaximum()
    {
        // Arrange
        var policy = LatencyPolicyFactory.Create();

        // Act
        var delta = policy.MaximumAcceptableDelta(ZeroWait);

        // Assert
        delta.Milliseconds.Should().Be(DefaultLatencyDeltaMs);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenOneIntervalElapsed_ShouldExpandByStep()
    {
        // Arrange
        var policy = LatencyPolicyFactory.Create();

        // Act
        var delta = policy.MaximumAcceptableDelta(OneExpansionInterval);

        // Assert
        delta.Milliseconds.Should().Be(ExpectedExpandedLatencyDeltaMs);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenWaitIsLong_ShouldCapAtAbsoluteMaximum()
    {
        // Arrange
        var policy = LatencyPolicyFactory.Create();

        // Act
        var delta = policy.MaximumAcceptableDelta(LongWait);

        // Assert
        delta.Milliseconds.Should().Be(AbsoluteMaxLatencyDeltaMs);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenExpansionStepIsZero_ShouldReturnBaseMaximum()
    {
        // Arrange
        var policy = LatencyPolicyFactory.Create(expansionStepMs: ZeroExpansionStepMs);

        // Act
        var delta = policy.MaximumAcceptableDelta(OneExpansionInterval);

        // Assert
        delta.Milliseconds.Should().Be(DefaultLatencyDeltaMs);
    }

    [Fact]
    public void Default_WhenAccessed_ShouldUseBuiltInDefaults()
    {
        // Act
        var policy = LatencyPolicy.Default;

        // Assert
        policy.BaseMaximumDelta.Milliseconds.Should().Be(LatencyPolicy.DefaultBaseMaxLatencyDeltaMs);
        policy.ExpansionInterval.Should().Be(TimeSpan.FromSeconds(LatencyPolicy.DefaultExpansionIntervalSeconds));
        policy.ExpansionStep.Milliseconds.Should().Be(LatencyPolicy.DefaultExpansionStepMs);
        policy.AbsoluteMaximumDelta.Milliseconds.Should().Be(LatencyPolicy.DefaultAbsoluteMaxLatencyDeltaMs);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenAbsoluteEqualsBase_ShouldIgnoreWaitTime()
    {
        // Arrange
        var policy = LatencyPolicyFactory.Create(
            baseMaxLatencyDeltaMs: DefaultLatencyDeltaMs,
            absoluteMaxLatencyDeltaMs: DefaultLatencyDeltaMs);

        // Act
        var delta = policy.MaximumAcceptableDelta(LongWait);

        // Assert
        delta.Milliseconds.Should().Be(DefaultLatencyDeltaMs);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenWaitIsPartialInterval_ShouldReturnBaseMaximum()
    {
        // Arrange
        var policy = LatencyPolicyFactory.Create();

        // Act
        var delta = policy.MaximumAcceptableDelta(PartialExpansionInterval);

        // Assert
        delta.Milliseconds.Should().Be(DefaultLatencyDeltaMs);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenExactCapStepsElapsed_ShouldReturnAbsoluteMaximum()
    {
        // Arrange: base 50, step 25, abs 100 → 2 steps reach the cap.
        var policy = LatencyPolicyFactory.Create();

        // Act
        var delta = policy.MaximumAcceptableDelta(ExactCapExpansionWait);

        // Assert
        delta.Milliseconds.Should().Be(AbsoluteMaxLatencyDeltaMs);
    }

    [Fact]
    public void Create_WhenTypedExpansionIntervalIsInvalid_ShouldFail()
    {
        // Arrange
        var baseDelta = LatencyDeltaFactory.Create(DefaultLatencyDeltaMs);
        var step = LatencyDeltaFactory.Create(DefaultExpansionStepMs);
        var absolute = LatencyDeltaFactory.Create(AbsoluteMaxLatencyDeltaMs);

        // Act
        var result = LatencyPolicy.Create(
            baseDelta,
            TimeSpan.Zero,
            step,
            absolute);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyPolicy);
    }

    [Fact]
    public void Create_WhenTypedAbsoluteIsBelowBase_ShouldFail()
    {
        // Arrange
        var baseDelta = LatencyDeltaFactory.Create(BaseMaxAboveAbsoluteBaseMs);
        var step = LatencyDeltaFactory.Create(DefaultExpansionStepMs);
        var absolute = LatencyDeltaFactory.Create(BaseMaxAboveAbsoluteAbsoluteMs);

        // Act
        var result = LatencyPolicy.Create(
            baseDelta,
            TimeSpan.FromSeconds(DefaultExpansionIntervalSeconds),
            step,
            absolute);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyPolicy);
    }

    [Fact]
    public void Create_WhenExpansionStepIsInvalid_ShouldFail()
    {
        // Act
        var result = LatencyPolicy.Create(
            DefaultLatencyDeltaMs,
            DefaultExpansionIntervalSeconds,
            NegativeLatencyDeltaMs,
            AbsoluteMaxLatencyDeltaMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyDelta);
    }

    [Fact]
    public void Create_WhenAbsoluteDeltaIsInvalid_ShouldFail()
    {
        // Act
        var result = LatencyPolicy.Create(
            DefaultLatencyDeltaMs,
            DefaultExpansionIntervalSeconds,
            DefaultExpansionStepMs,
            NegativeLatencyDeltaMs);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidLatencyDelta);
    }

    [Fact]
    public void MaximumAcceptableDelta_WhenWaitIsNegative_ShouldReturnBaseMaximum()
    {
        // Arrange
        var policy = LatencyPolicyFactory.Create();

        // Act
        var delta = policy.MaximumAcceptableDelta(TimeSpan.FromSeconds(-1));

        // Assert
        delta.Milliseconds.Should().Be(DefaultLatencyDeltaMs);
    }
}
