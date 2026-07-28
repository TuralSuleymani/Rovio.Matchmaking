namespace Rovio.Matchmaking.Application.Services.Contracts;

public interface ISessionService
{
    Task<Result<SessionDto, IDomainError>> GetAsync(Id<GameSession> sessionId, CancellationToken cancellationToken = default);
    Task<Result<SessionDto, IDomainError>> LateJoinAsync(Id<GameSession> sessionId, LateJoinRequest request, CancellationToken cancellationToken = default);
}
