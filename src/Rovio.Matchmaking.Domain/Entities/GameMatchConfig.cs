using System.Security.Cryptography;
using System.Text;

namespace Rovio.Matchmaking.Domain.Entities;

public sealed class GameMatchConfig : Common.Entity<GameMatchConfig>
{
    public const string AngryBirds2GameIdValue = "angry-birds-2";

    private GameMatchConfig(
        Id<GameMatchConfig> id,
        GameId gameId,
        PlayerCapacity playerCapacity,
        bool allowLateJoin,
        bool enabled,
        int? maxQueueDepth,
        LatencyPolicy latencyPolicy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id, createdAt, updatedAt)
    {
        GameId = gameId;
        PlayerCapacity = playerCapacity;
        AllowLateJoin = allowLateJoin;
        Enabled = enabled;
        MaxQueueDepth = maxQueueDepth;
        LatencyPolicy = latencyPolicy;
    }

    public GameId GameId { get; }
    public PlayerCapacity PlayerCapacity { get; }
    public bool AllowLateJoin { get; }
    public bool Enabled { get; }
    public int? MaxQueueDepth { get; }
    public LatencyPolicy LatencyPolicy { get; }
    public DateTimeOffset CreatedAt => CreatedAtUtc;
    public DateTimeOffset UpdatedAt => LastModifiedAtUtc;

    public static Result<GameMatchConfig, DomainError> Create(
        string gameId,
        int minPlayers,
        int maxPlayers,
        bool allowLateJoin,
        bool enabled,
        int? maxQueueDepth,
        LatencyPolicy latencyPolicy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var gameIdResult = GameId.Create(gameId);
        if (gameIdResult.IsFailure)
        {
            return gameIdResult.Error;
        }

        return Create(
            gameIdResult.Value,
            minPlayers,
            maxPlayers,
            allowLateJoin,
            enabled,
            maxQueueDepth,
            latencyPolicy,
            createdAt,
            updatedAt);
    }

    public static Result<GameMatchConfig, DomainError> Create(
        GameId gameId,
        int minPlayers,
        int maxPlayers,
        bool allowLateJoin,
        bool enabled,
        int? maxQueueDepth,
        LatencyPolicy latencyPolicy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var capacityResult = PlayerCapacity.Create(minPlayers, maxPlayers);
        if (capacityResult.IsFailure)
        {
            return capacityResult.Error;
        }

        if (maxQueueDepth is <= 0)
        {
            return DomainError.Validation("MaxQueueDepth must be > 0 when set.", code: DomainErrorCodes.InvalidConfig);
        }

        return new GameMatchConfig(
            ToEntityId(gameId),
            gameId,
            capacityResult.Value,
            allowLateJoin,
            enabled,
            maxQueueDepth,
            latencyPolicy,
            createdAt,
            updatedAt);
    }

    public static GameMatchConfig CreateAngryBirds2Defaults(DateTimeOffset now) =>
        Create(
            AngryBirds2GameIdValue,
            2,
            4,
            true,
            true,
            null,
            LatencyPolicy.Default,
            now,
            now).Value;

    private static Id<GameMatchConfig> ToEntityId(GameId gameId)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes("mm:game-match-config:" + gameId.Value));
        return Id<GameMatchConfig>.Create(new Guid(hash)).Value;
    }
}
