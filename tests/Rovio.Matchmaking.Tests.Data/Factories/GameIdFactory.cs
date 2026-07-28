
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class GameIdFactory
{
    public static GameId Create(string? value = null) =>
        GameId.Create(value ?? AngryBirds2GameId).Value;
}
