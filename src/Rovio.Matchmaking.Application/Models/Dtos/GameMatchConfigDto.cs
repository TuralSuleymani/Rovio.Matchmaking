namespace Rovio.Matchmaking.Application.Models.Dtos;

public sealed record GameMatchConfigDto(
    string GameId,
    int MinPlayers,
    int MaxPlayers,
    bool AllowLateJoin,
    bool Enabled,
    int? MaxQueueDepth,
    LatencyPolicyDto LatencyPolicy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
