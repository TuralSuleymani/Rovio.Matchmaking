
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class GameMatchConfigFactory
{
    public static GameMatchConfig Create(
        string? gameId = null,
        int? minPlayers = null,
        int? maxPlayers = null,
        bool allowLateJoin = true,
        bool enabled = true,
        int? maxQueueDepth = null,
        LatencyPolicy? latencyPolicy = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null) =>
        GameMatchConfig.Create(
            gameId ?? AngryBirds2GameId,
            minPlayers ?? DefaultMinPlayers,
            maxPlayers ?? DefaultMaxPlayers,
            allowLateJoin,
            enabled,
            maxQueueDepth,
            latencyPolicy ?? LatencyPolicyFactory.CreateDefault(),
            createdAt ?? DefaultNow,
            updatedAt ?? DefaultNow).Value;

    public static GameMatchConfig CreateDuo(
        string? gameId = null,
        LatencyPolicy? latencyPolicy = null) =>
        Create(
            gameId: gameId,
            maxPlayers: DuoMaxPlayers,
            latencyPolicy: latencyPolicy);

    public static GameMatchConfig CreateAngryBirds2Defaults() =>
        GameMatchConfig.CreateAngryBirds2Defaults(DefaultNow);
}
