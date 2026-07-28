namespace Rovio.Matchmaking.Application.Services.Contracts;

public interface IQueueService
{
    Task<UnitResult<IDomainError>> CancelAsync(
        GameId gameId,
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default);

    Task<Result<(TicketDto Ticket, bool Created), IDomainError>> EnqueueAsync(
        GameId gameId,
        EnqueueRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<TicketDto, IDomainError>> GetTicketAsync(
        GameId gameId,
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default);
}
