
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class PlayerCapacityFactory
{
    public static PlayerCapacity Create(
        int? minPlayerCount = null,
        int? maxPlayerCount = null) =>
        PlayerCapacity.Create(
            minPlayerCount ?? DefaultMinPlayers,
            maxPlayerCount ?? DefaultMaxPlayers).Value;

    public static PlayerCapacity CreateDuo() =>
        Create(DefaultMinPlayers, DuoMaxPlayers);

    public static PlayerCapacity CreateTrio() =>
        Create(DefaultMinPlayers, TrioMaxPlayers);
}
