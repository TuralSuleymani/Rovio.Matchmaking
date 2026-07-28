namespace Rovio.Matchmaking.Domain.ValueObjects;

/// <summary>
/// Expands acceptable latency delta as wait time grows so stranded players still match.
/// </summary>
public sealed record LatencyPolicy
{
    public const int DefaultBaseMaxLatencyDeltaMs = 50;
    public const int DefaultExpansionIntervalSeconds = 10;
    public const int DefaultExpansionStepMs = 25;
    public const int DefaultAbsoluteMaxLatencyDeltaMs = 200;

    private LatencyPolicy(
        LatencyDelta baseMaximumDelta,
        TimeSpan expansionInterval,
        LatencyDelta expansionStep,
        LatencyDelta absoluteMaximumDelta)
    {
        BaseMaximumDelta = baseMaximumDelta;
        ExpansionInterval = expansionInterval;
        ExpansionStep = expansionStep;
        AbsoluteMaximumDelta = absoluteMaximumDelta;
    }

    public static LatencyPolicy Default { get; } =
        new(
            LatencyDelta.FromValidated(
                DefaultBaseMaxLatencyDeltaMs),
            TimeSpan.FromSeconds(
                DefaultExpansionIntervalSeconds),
            LatencyDelta.FromValidated(
                DefaultExpansionStepMs),
            LatencyDelta.FromValidated(
                DefaultAbsoluteMaxLatencyDeltaMs));

    public LatencyDelta BaseMaximumDelta { get; }

    public TimeSpan ExpansionInterval { get; }

    public LatencyDelta ExpansionStep { get; }

    public LatencyDelta AbsoluteMaximumDelta { get; }

    public static Result<LatencyPolicy, DomainError> Create(
        LatencyDelta baseMaximumDelta,
        TimeSpan expansionInterval,
        LatencyDelta expansionStep,
        LatencyDelta absoluteMaximumDelta)
    {
        if (expansionInterval <= TimeSpan.Zero)
        {
            return DomainError.Validation(
                "Expansion interval must be greater than zero.",
                code: DomainErrorCodes.InvalidLatencyPolicy);
        }

        if (absoluteMaximumDelta.Milliseconds <
            baseMaximumDelta.Milliseconds)
        {
            return DomainError.Validation(
                "Absolute maximum latency delta must be greater than or equal to the base maximum latency delta.",
                code: DomainErrorCodes.InvalidLatencyPolicy);
        }

        return new LatencyPolicy(
            baseMaximumDelta,
            expansionInterval,
            expansionStep,
            absoluteMaximumDelta);
    }

    public static Result<LatencyPolicy, DomainError> Create(
        int baseMaxLatencyDeltaMs,
        int expansionIntervalSeconds,
        int expansionStepMs,
        int absoluteMaxLatencyDeltaMs)
    {
        if (expansionIntervalSeconds <= 0)
        {
            return DomainError.Validation(
                "Expansion interval must be greater than zero.",
                code: DomainErrorCodes.InvalidLatencyPolicy);
        }

        var baseDeltaResult =
            LatencyDelta.Create(baseMaxLatencyDeltaMs);

        if (baseDeltaResult.IsFailure)
        {
            return baseDeltaResult.Error;
        }

        var expansionStepResult =
            LatencyDelta.Create(expansionStepMs);

        if (expansionStepResult.IsFailure)
        {
            return expansionStepResult.Error;
        }

        var absoluteMaximumResult =
            LatencyDelta.Create(absoluteMaxLatencyDeltaMs);

        if (absoluteMaximumResult.IsFailure)
        {
            return absoluteMaximumResult.Error;
        }

        return Create(
            baseDeltaResult.Value,
            TimeSpan.FromSeconds(expansionIntervalSeconds),
            expansionStepResult.Value,
            absoluteMaximumResult.Value);
    }

    public LatencyDelta MaximumAcceptableDelta(
        TimeSpan waitTime)
    {
        if (waitTime <= TimeSpan.Zero ||
            ExpansionStep.Milliseconds == 0)
        {
            return BaseMaximumDelta;
        }

        var elapsedSteps =
            waitTime.Ticks / ExpansionInterval.Ticks;

        var availableExpansion =
            AbsoluteMaximumDelta.Milliseconds -
            BaseMaximumDelta.Milliseconds;

        if (availableExpansion == 0)
        {
            return AbsoluteMaximumDelta;
        }

        var stepsRequiredToReachMaximum =
            (
                availableExpansion +
                ExpansionStep.Milliseconds -
                1L
            ) / ExpansionStep.Milliseconds;

        if (elapsedSteps >= stepsRequiredToReachMaximum)
        {
            return AbsoluteMaximumDelta;
        }

        var expansion =
            elapsedSteps * ExpansionStep.Milliseconds;

        var expandedMilliseconds =
            BaseMaximumDelta.Milliseconds +
            (int)expansion;

        return LatencyDelta.FromValidated(
            expandedMilliseconds);
    }
}
