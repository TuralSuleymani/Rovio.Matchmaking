
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class GameSessionFactory
{
    public static GameSession Create(
        GameId? gameId = null,
        MatchRegion? region = null,
        PlayerCapacity? playerCapacity = null,
        bool allowLateJoin = true,
        IReadOnlyList<PlayerId>? playerIds = null,
        DateTimeOffset? now = null) =>
        GameSession.Create(
            gameId ?? GameIdFactory.Create(),
            region ?? MatchRegionFactory.Create(),
            playerCapacity ?? PlayerCapacityFactory.Create(),
            allowLateJoin,
            playerIds ??
            [
                PlayerIdFactory.Create(),
                PlayerIdFactory.CreateSecond()
            ],
            now ?? DefaultNow).Value;

    public static GameSession CreateFull(
        bool allowLateJoin = true) =>
        Create(
            playerCapacity: PlayerCapacityFactory.CreateDuo(),
            allowLateJoin: allowLateJoin,
            playerIds:
            [
                PlayerIdFactory.Create(),
                PlayerIdFactory.CreateSecond()
            ]);

    public static GameSession CreateWithOpenSlot(
        bool allowLateJoin = true) =>
        Create(
            playerCapacity: PlayerCapacityFactory.CreateTrio(),
            allowLateJoin: allowLateJoin,
            playerIds:
            [
                PlayerIdFactory.Create(),
                PlayerIdFactory.CreateSecond()
            ]);

    public static GameSession Rehydrate(
        SessionStatus status,
        bool allowLateJoin,
        PlayerCapacity? playerCapacity = null,
        IReadOnlyList<PlayerId>? playerIds = null) =>
        GameSession.Rehydrate(
            Id<GameSession>.New(),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            playerCapacity ?? PlayerCapacityFactory.CreateTrio(),
            status,
            allowLateJoin,
            playerIds ??
            [
                PlayerIdFactory.Create(),
                PlayerIdFactory.CreateSecond()
            ],
            DefaultNow,
            DefaultNow).Value;
}
