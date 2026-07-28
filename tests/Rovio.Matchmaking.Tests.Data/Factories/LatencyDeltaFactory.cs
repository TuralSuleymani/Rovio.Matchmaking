
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class LatencyDeltaFactory
{
    public static LatencyDelta Create(int? milliseconds = null) =>
        LatencyDelta.Create(milliseconds ?? DefaultLatencyDeltaMs).Value;
}
