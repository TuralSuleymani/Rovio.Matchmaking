namespace Rovio.Matchmaking.Application.Abstractions;

public sealed record EnqueueResult(MatchTicket Ticket, bool Created);

public interface ITicketStore
{
    Task<Result<EnqueueResult, IDomainError>> EnqueueAsync(
        GameId gameId,
        PlayerId playerId,
        MatchRegion region,
        int latencyMs,
        int maxQueueDepth,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default);

    Task<Result<MatchTicket, IDomainError>> GetAsync(
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default);

    Task<UnitResult<IDomainError>> CancelAsync(
        GameId gameId,
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<MatchTicket>, IDomainError>> GetQueuedCandidatesAsync(
        GameId gameId,
        MatchRegion region,
        int limit,
        CancellationToken cancellationToken = default);
}
