namespace Rovio.Matchmaking.Application.Models.Dtos;

public sealed record SessionDto(
    string SessionId,
    string GameId,
    string Region,
    string Status,
    int MaxPlayers,
    bool AllowLateJoin,
    IReadOnlyList<string> PlayerIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset StartedAt);
