
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class PlayerIdFactory
{
    public static PlayerId Create(string? value = null) =>
        PlayerId.Create(value ?? DefaultPlayerId).Value;

    public static PlayerId CreateSecond() => Create(SecondPlayerId);

    public static PlayerId CreateThird() => Create(ThirdPlayerId);

    public static PlayerId CreateFourth() => Create(FourthPlayerId);

    public static PlayerId CreateFifth() => Create(FifthPlayerId);
}
