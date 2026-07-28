
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class LatencyFactory
{
    public static Latency Create(int? milliseconds = null) =>
        Latency.Create(milliseconds ?? DefaultLatencyMs).Value;
}
