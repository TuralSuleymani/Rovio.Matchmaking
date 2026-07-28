namespace Rovio.Matchmaking.Domain.Entities;

public sealed class GameSession : Common.Entity<GameSession>
{
    private readonly List<PlayerId> _playerIds;

    private GameSession(
        Id<GameSession> id,
        GameId gameId,
        MatchRegion region,
        PlayerCapacity playerCapacity,
        SessionStatus status,
        bool allowLateJoin,
        IReadOnlyList<PlayerId> playerIds,
        DateTimeOffset createdAt,
        DateTimeOffset startedAt)
        : base(id, createdAt, createdAt)
    {
        GameId = gameId;
        Region = region;
        PlayerCapacity = playerCapacity;
        Status = status;
        AllowLateJoin = allowLateJoin;
        _playerIds = [.. playerIds];
        StartedAt = startedAt;
    }

    public GameId GameId { get; }
    public MatchRegion Region { get; }
    public PlayerCapacity PlayerCapacity { get; }
    public SessionStatus Status { get; private set; }

    public bool AllowLateJoin { get; }

    public IReadOnlyList<PlayerId> PlayerIds => _playerIds;
    public DateTimeOffset StartedAt { get; }

    public int MaxPlayers => PlayerCapacity.MaxPlayerCount;

    public int OpenSlots =>
        Math.Max(0, PlayerCapacity.MaxPlayerCount - _playerIds.Count);

    public bool CanLateJoin =>
        AllowLateJoin &&
        Status != SessionStatus.Full &&
        OpenSlots > 0;

    public static Result<GameSession, DomainError> Create(
        GameId gameId,
        MatchRegion region,
        PlayerCapacity playerCapacity,
        bool allowLateJoin,
        IReadOnlyList<PlayerId> playerIds,
        DateTimeOffset now,
        Id<GameSession>? id = null)
    {
        if (region is null)
        {
            return DomainError.Validation("Match region is required.", code: DomainErrorCodes.InvalidSession);
        }

        if (playerCapacity is null)
        {
            return DomainError.Validation("Player capacity is required.", code: DomainErrorCodes.InvalidSession);
        }

        if (playerIds is null)
        {
            return DomainError.Validation("Players are required.", code: DomainErrorCodes.InvalidSession);
        }

        var uniquePlayerIds = playerIds.Distinct().ToArray();
        if (uniquePlayerIds.Length != playerIds.Count)
        {
            return DomainError.Conflict(
                "A player cannot appear more than once in a session.",
                DomainErrorCodes.DuplicateSessionPlayer);
        }

        if (uniquePlayerIds.Length < playerCapacity.MinPlayerCount)
        {
            return DomainError.Validation(
                $"Session must include at least {playerCapacity.MinPlayerCount} players.",
                code: DomainErrorCodes.InsufficientPlayers);
        }

        if (uniquePlayerIds.Length > playerCapacity.MaxPlayerCount)
        {
            return DomainError.Validation(
                $"Session cannot contain more than {playerCapacity.MaxPlayerCount} players.",
                code: DomainErrorCodes.PlayerCapacityExceeded);
        }

        var status = uniquePlayerIds.Length == playerCapacity.MaxPlayerCount
            ? SessionStatus.Full
            : SessionStatus.Formed;

        return new GameSession(
            id ?? Id<GameSession>.New(),
            gameId,
            region,
            playerCapacity,
            status,
            allowLateJoin,
            uniquePlayerIds,
            now,
            now);
    }

    public static Result<GameSession, DomainError> Rehydrate(
        Id<GameSession> id,
        GameId gameId,
        MatchRegion region,
        PlayerCapacity playerCapacity,
        SessionStatus status,
        bool allowLateJoin,
        IReadOnlyList<PlayerId> playerIds,
        DateTimeOffset createdAt,
        DateTimeOffset startedAt)
    {
        if (playerIds is null)
        {
            return DomainError.Validation("Players are required.", code: DomainErrorCodes.InvalidSession);
        }

        var uniquePlayerIds = playerIds.Distinct().ToArray();
        if (uniquePlayerIds.Length != playerIds.Count)
        {
            return DomainError.Conflict(
                "A player cannot appear more than once in a session.",
                DomainErrorCodes.DuplicateSessionPlayer);
        }

        return new GameSession(
            id,
            gameId,
            region,
            playerCapacity,
            status,
            allowLateJoin,
            uniquePlayerIds,
            createdAt,
            startedAt);
    }

    public UnitResult<DomainError> EnsureCanLateJoin()
    {
        if (!AllowLateJoin)
        {
            return DomainError.Validation(
                "This session does not allow late join.",
                code: DomainErrorCodes.LateJoinDisabled);
        }

        if (Status == SessionStatus.Full || OpenSlots == 0)
        {
            return DomainError.Conflict("Session has no open slots.", DomainErrorCodes.SessionFull);
        }

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> TryAddPlayer(PlayerId playerId)
    {
        var canJoin = EnsureCanLateJoin();
        if (canJoin.IsFailure)
        {
            return canJoin.Error;
        }

        if (_playerIds.Contains(playerId))
        {
            return DomainError.Conflict(
                "A player cannot appear more than once in a session.",
                DomainErrorCodes.DuplicateSessionPlayer);
        }

        _playerIds.Add(playerId);
        Status = _playerIds.Count >= PlayerCapacity.MaxPlayerCount
            ? SessionStatus.Full
            : SessionStatus.Formed;

        return UnitResult.Success<DomainError>();
    }
}
