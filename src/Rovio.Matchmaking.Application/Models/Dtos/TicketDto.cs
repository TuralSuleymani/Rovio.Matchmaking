namespace Rovio.Matchmaking.Application.Models.Dtos;

public sealed record TicketDto(
    string TicketId,
    string PlayerId,
    string GameId,
    string Region,
    int LatencyMs,
    DateTimeOffset EnqueuedAt,
    string Status,
    string? SessionId);
