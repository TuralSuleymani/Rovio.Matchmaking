namespace Rovio.Matchmaking.Application.Services.Implementations;

public sealed class QueueService(
    ITicketStore ticketStore,
    IGameConfigRuntime configRuntime,
    TimeProvider timeProvider,
    IOptions<MatchmakingOptions> options) : IQueueService
{
    private readonly MatchmakingOptions _options = options.Value;

    public async Task<Result<(TicketDto Ticket, bool Created), IDomainError>> EnqueueAsync(
        GameId gameId,
        EnqueueRequest request,
        CancellationToken cancellationToken = default)
    {
        var playerIdResult = PlayerId.Create(request.PlayerId);
        if (playerIdResult.IsFailure)
        {
            return Result.Failure<(TicketDto Ticket, bool Created), IDomainError>(playerIdResult.Error);
        }

        var regionResult = MatchRegion.Create(request.Region);
        if (regionResult.IsFailure)
        {
            return Result.Failure<(TicketDto Ticket, bool Created), IDomainError>(regionResult.Error);
        }

        PlayerId playerId = playerIdResult.Value;
        MatchRegion region = regionResult.Value;

        GameMatchConfig? config;
        try
        {
            config = await configRuntime.GetAsync(gameId, cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<(TicketDto Ticket, bool Created), IDomainError>(
                DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable"));
        }

        if (config is null)
        {
            return Result.Failure<(TicketDto Ticket, bool Created), IDomainError>(
                DomainError.NotFound($"Game '{gameId}' was not found in runtime config.", "game_not_found"));
        }

        if (!config.Enabled)
        {
            return Result.Failure<(TicketDto Ticket, bool Created), IDomainError>(
                DomainError.BadRequest($"Game '{gameId}' is disabled.", "game_disabled"));
        }

        var maxDepth = config.MaxQueueDepth ?? _options.MaxQueueDepth;
        var enqueue = await ticketStore.EnqueueAsync(
            gameId,
            playerId,
            region,
            request.LatencyMs,
            maxDepth,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (enqueue.IsFailure)
        {
            return Result.Failure<(TicketDto Ticket, bool Created), IDomainError>(enqueue.Error);
        }

        return (enqueue.Value.Ticket.ToDto(), enqueue.Value.Created);
    }

    public async Task<Result<TicketDto, IDomainError>> GetTicketAsync(
        GameId gameId,
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticketResult = await ticketStore.GetAsync(ticketId, cancellationToken);
        if (ticketResult.IsFailure)
        {
            return Result.Failure<TicketDto, IDomainError>(ticketResult.Error);
        }

        var ticket = ticketResult.Value;
        if (ticket.GameId != gameId)
        {
            return Result.Failure<TicketDto, IDomainError>(
                DomainError.NotFound($"Ticket '{ticketId}' was not found for game '{gameId}'.", "ticket_not_found"));
        }

        return ticket.ToDto();
    }

    public async Task<UnitResult<IDomainError>> CancelAsync(
        GameId gameId,
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticketResult = await ticketStore.GetAsync(ticketId, cancellationToken);
        if (ticketResult.IsFailure)
        {
            return UnitResult.Failure(ticketResult.Error);
        }

        var ticket = ticketResult.Value;
        if (ticket.GameId != gameId)
        {
            return UnitResult.Failure<IDomainError>(
                DomainError.NotFound($"Ticket '{ticketId}' was not found for game '{gameId}'.", "ticket_not_found"));
        }

        if (ticket.Status == TicketStatus.Matched)
        {
            return UnitResult.Failure<IDomainError>(
                DomainError.Conflict("Ticket has already been matched.", "already_matched"));
        }

        if (ticket.Status == TicketStatus.Cancelled)
        {
            return UnitResult.Failure<IDomainError>(
                DomainError.Conflict("Ticket has already been cancelled.", "already_cancelled"));
        }

        return await ticketStore.CancelAsync(gameId, ticketId, cancellationToken);
    }
}
