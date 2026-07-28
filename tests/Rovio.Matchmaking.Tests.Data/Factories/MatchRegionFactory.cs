
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class MatchRegionFactory
{
    public static MatchRegion Create(string? value = null) =>
        MatchRegion.Create(value ?? DefaultRegion).Value;
}
