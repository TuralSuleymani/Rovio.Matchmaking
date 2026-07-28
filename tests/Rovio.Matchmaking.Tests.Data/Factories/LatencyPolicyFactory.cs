
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class LatencyPolicyFactory
{
    public static LatencyPolicy Create(
        int? baseMaxLatencyDeltaMs = null,
        int? expansionIntervalSeconds = null,
        int? expansionStepMs = null,
        int? absoluteMaxLatencyDeltaMs = null) =>
        LatencyPolicy.Create(
            baseMaxLatencyDeltaMs ?? DefaultLatencyDeltaMs,
            expansionIntervalSeconds ?? DefaultExpansionIntervalSeconds,
            expansionStepMs ?? DefaultExpansionStepMs,
            absoluteMaxLatencyDeltaMs ?? AbsoluteMaxLatencyDeltaMs).Value;

    public static LatencyPolicy CreateDefault() => LatencyPolicy.Default;
}
