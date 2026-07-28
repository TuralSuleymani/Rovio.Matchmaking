namespace Rovio.Matchmaking.Application.Abstractions;

public interface ISessionStore
{
    Task<Result<GameSession, IDomainError>> GetAsync(
        Id<GameSession> sessionId,
        CancellationToken cancellationToken = default);

    Task<UnitResult<IDomainError>> FormSessionAsync(
        GameSession session,
        IReadOnlyList<Id<MatchTicket>> ticketIds,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<GameSession>, IDomainError>> GetOpenSessionsAsync(
        GameId gameId,
        MatchRegion region,
        CancellationToken cancellationToken = default);

    Task<UnitResult<IDomainError>> LateJoinAsync(
        Id<GameSession> sessionId,
        GameId gameId,
        MatchRegion region,
        Id<MatchTicket> ticketId,
        PlayerId playerId,
        CancellationToken cancellationToken = default);
}
