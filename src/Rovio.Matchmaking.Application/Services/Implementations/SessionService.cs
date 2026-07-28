namespace Rovio.Matchmaking.Application.Services.Implementations;

public sealed class SessionService(
    ISessionStore sessionStore,
    ITicketStore ticketStore,
    IGameConfigRuntime configRuntime,
    IQueueService queueService) : ISessionService
{
    public async Task<Result<SessionDto, IDomainError>> GetAsync(
        Id<GameSession> sessionId,
        CancellationToken cancellationToken = default)
    {
        var sessionResult = await sessionStore.GetAsync(sessionId, cancellationToken);
        if (sessionResult.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(sessionResult.Error);
        }

        return sessionResult.Value.ToDto();
    }

    public async Task<Result<SessionDto, IDomainError>> LateJoinAsync(
        Id<GameSession> sessionId,
        LateJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        var playerIdResult = PlayerId.Create(request.PlayerId);
        if (playerIdResult.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(playerIdResult.Error);
        }

        PlayerId playerId = playerIdResult.Value;

        var sessionResult = await sessionStore.GetAsync(sessionId, cancellationToken);
        if (sessionResult.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(sessionResult.Error);
        }

        var session = sessionResult.Value;
        var canJoin = session.EnsureCanLateJoin();
        if (canJoin.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(canJoin.Error);
        }

        try
        {
            var config = await configRuntime.GetAsync(session.GameId, cancellationToken);
            if (config is null)
            {
                return Result.Failure<SessionDto, IDomainError>(
                    DomainError.NotFound($"Game '{session.GameId}' was not found.", "game_not_found"));
            }
        }
        catch (Exception)
        {
            return Result.Failure<SessionDto, IDomainError>(
                DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable"));
        }

        if (!string.Equals(request.Region.Trim(), session.Region.Value, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SessionDto, IDomainError>(
                DomainError.BadRequest("Late join region must match the session region.", "region_mismatch"));
        }

        var enqueue = await queueService.EnqueueAsync(
            session.GameId,
            new EnqueueRequest(playerId.Value, session.Region.Value, request.LatencyMs),
            cancellationToken);

        if (enqueue.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(enqueue.Error);
        }

        var ticketIdResult = Id<MatchTicket>.Create(enqueue.Value.Ticket.TicketId);
        if (ticketIdResult.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(
                DomainError.Unexpected("Ticket id from enqueue was invalid.", "invalid_ticket_id"));
        }

        var ticketResult = await ticketStore.GetAsync(ticketIdResult.Value, cancellationToken);
        if (ticketResult.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(ticketResult.Error);
        }

        var ticket = ticketResult.Value;
        if (ticket.Status == TicketStatus.Matched && ticket.SessionId == sessionId)
        {
            var refreshed = await sessionStore.GetAsync(sessionId, cancellationToken);
            return refreshed.IsFailure
                ? Result.Failure<SessionDto, IDomainError>(refreshed.Error)
                : refreshed.Value.ToDto();
        }

        if (ticket.Status != TicketStatus.Queued)
        {
            return Result.Failure<SessionDto, IDomainError>(
                DomainError.Conflict(
                    "Player ticket is not in a queued state for late join.",
                    "ticket_not_queued"));
        }

        var lateJoin = await sessionStore.LateJoinAsync(
            sessionId,
            session.GameId,
            session.Region,
            ticket.Id,
            ticket.PlayerId,
            cancellationToken);

        if (lateJoin.IsFailure)
        {
            return Result.Failure<SessionDto, IDomainError>(lateJoin.Error);
        }

        var updated = await sessionStore.GetAsync(sessionId, cancellationToken);
        return updated.IsFailure
            ? Result.Failure<SessionDto, IDomainError>(updated.Error)
            : updated.Value.ToDto();
    }
}
