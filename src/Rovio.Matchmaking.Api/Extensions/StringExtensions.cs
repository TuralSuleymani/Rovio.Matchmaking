using CSharpFunctionalExtensions;
using Rovio.Domain.Common;
using Rovio.Domain.Common.Errors;
using Rovio.Matchmaking.Domain.Entities;
using Rovio.Matchmaking.Domain.ValueObjects;

namespace Rovio.Matchmaking.Api.Extensions;

public static class StringExtensions
{
    public static Result<GameId, IDomainError> ParseGameId(this string? gameId)
    {
        var result = GameId.Create(gameId);
        return result.IsFailure
            ? Result.Failure<GameId, IDomainError>(result.Error)
            : result.Value;
    }

    public static Result<Id<GameSession>, IDomainError> ParseSessionId(this string? sessionId)
    {
        var result = Id<GameSession>.Create(sessionId);
        return result.IsFailure
            ? Result.Failure<Id<GameSession>, IDomainError>(
                DomainError.BadRequest("Session id is invalid.", "invalid_session_id"))
            : result.Value;
    }

    public static Result<Id<MatchTicket>, IDomainError> ParseTicketId(this string? ticketId)
    {
        var result = Id<MatchTicket>.Create(ticketId);
        return result.IsFailure
            ? Result.Failure<Id<MatchTicket>, IDomainError>(
                DomainError.BadRequest("Ticket id is invalid.", "invalid_ticket_id"))
            : result.Value;
    }
}
