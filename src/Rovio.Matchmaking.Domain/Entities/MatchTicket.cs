namespace Rovio.Matchmaking.Domain.Entities;

public sealed class MatchTicket : Common.Entity<MatchTicket>
{
    private MatchTicket(
        Id<MatchTicket> id,
        PlayerId playerId,
        GameId gameId,
        MatchRegion region,
        Latency latency,
        DateTimeOffset enqueuedAt,
        TicketStatus status,
        Id<GameSession>? sessionId)
        : base(id, enqueuedAt, enqueuedAt)
    {
        PlayerId = playerId;
        GameId = gameId;
        Region = region;
        Latency = latency;
        EnqueuedAt = enqueuedAt;
        Status = status;
        SessionId = sessionId;
    }

    public PlayerId PlayerId { get; }
    public GameId GameId { get; }
    public MatchRegion Region { get; }
    public Latency Latency { get; }
    public DateTimeOffset EnqueuedAt { get; }
    public TicketStatus Status { get; private set; }
    public Id<GameSession>? SessionId { get; private set; }

    public TimeSpan WaitTime(DateTimeOffset now) =>
        now <= EnqueuedAt ? TimeSpan.Zero : now - EnqueuedAt;

    public static Result<MatchTicket, DomainError> CreateQueued(
        string playerId,
        string gameId,
        string region,
        int latencyMs,
        DateTimeOffset enqueuedAt,
        Id<MatchTicket>? id = null)
    {
        var playerIdResult = PlayerId.Create(playerId);
        if (playerIdResult.IsFailure)
        {
            return playerIdResult.Error;
        }

        var gameIdResult = GameId.Create(gameId);
        if (gameIdResult.IsFailure)
        {
            return gameIdResult.Error;
        }

        var regionResult = MatchRegion.Create(region);
        if (regionResult.IsFailure)
        {
            return regionResult.Error;
        }

        var latencyResult = Latency.Create(latencyMs);
        if (latencyResult.IsFailure)
        {
            return latencyResult.Error;
        }

        return CreateQueued(
            playerIdResult.Value,
            gameIdResult.Value,
            regionResult.Value,
            latencyResult.Value,
            enqueuedAt,
            id);
    }

    public static Result<MatchTicket, DomainError> CreateQueued(
        PlayerId playerId,
        GameId gameId,
        MatchRegion region,
        Latency latency,
        DateTimeOffset enqueuedAt,
        Id<MatchTicket>? id = null)
    {
        if (playerId is null)
        {
            return DomainError.Validation("Player id is required.", code: DomainErrorCodes.InvalidPlayerId);
        }

        if (gameId is null)
        {
            return DomainError.Validation("Game id is required.", code: DomainErrorCodes.InvalidGameId);
        }

        if (region is null)
        {
            return DomainError.Validation("Region is required.", code: DomainErrorCodes.InvalidRegion);
        }

        return new MatchTicket(
            id ?? Id<MatchTicket>.New(),
            playerId,
            gameId,
            region,
            latency,
            enqueuedAt,
            TicketStatus.Queued,
            sessionId: null);
    }

    public static Result<MatchTicket, DomainError> Rehydrate(
        Id<MatchTicket> id,
        PlayerId playerId,
        GameId gameId,
        MatchRegion region,
        Latency latency,
        DateTimeOffset enqueuedAt,
        TicketStatus status,
        Id<GameSession>? sessionId)
    {
        if (playerId is null)
        {
            return DomainError.Validation("Player id is required.", code: DomainErrorCodes.InvalidPlayerId);
        }

        if (gameId is null)
        {
            return DomainError.Validation("Game id is required.", code: DomainErrorCodes.InvalidGameId);
        }

        if (region is null)
        {
            return DomainError.Validation("Region is required.", code: DomainErrorCodes.InvalidRegion);
        }

        var stateError = ValidateStatusAndSession(status, sessionId);
        if (stateError is not null)
        {
            return stateError;
        }

        return new MatchTicket(
            id,
            playerId,
            gameId,
            region,
            latency,
            enqueuedAt,
            status,
            sessionId);
    }

    public UnitResult<DomainError> MarkMatched(Id<GameSession> sessionId)
    {
        if (Status != TicketStatus.Queued)
        {
            return DomainError.Conflict(
                "Only queued tickets can be matched.",
                DomainErrorCodes.InvalidTicketTransition);
        }

        Status = TicketStatus.Matched;
        SessionId = sessionId;
        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Cancel()
    {
        if (Status != TicketStatus.Queued)
        {
            return DomainError.Conflict(
                "Only queued tickets can be cancelled.",
                DomainErrorCodes.InvalidTicketTransition);
        }

        Status = TicketStatus.Cancelled;
        SessionId = null;
        return UnitResult.Success<DomainError>();
    }

    private static DomainError? ValidateStatusAndSession(
        TicketStatus status,
        Id<GameSession>? sessionId)
    {
        if (status == TicketStatus.Queued && sessionId is not null)
        {
            return DomainError.Validation(
                "Queued tickets cannot reference a session.",
                code: DomainErrorCodes.InvalidTicketState);
        }

        if (status == TicketStatus.Matched && sessionId is null)
        {
            return DomainError.Validation(
                "Matched tickets must reference a session.",
                code: DomainErrorCodes.InvalidTicketState);
        }

        if (status == TicketStatus.Cancelled && sessionId is not null)
        {
            return DomainError.Validation(
                "Cancelled tickets cannot reference a session.",
                code: DomainErrorCodes.InvalidTicketState);
        }

        return null;
    }
}
