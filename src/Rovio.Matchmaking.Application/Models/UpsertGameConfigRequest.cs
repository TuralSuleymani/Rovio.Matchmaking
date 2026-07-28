namespace Rovio.Matchmaking.Application.Models;

public sealed record UpsertGameConfigRequest(
    int MinPlayers,
    int MaxPlayers,
    bool AllowLateJoin,
    bool Enabled,
    int? MaxQueueDepth,
    LatencyPolicyDto? LatencyPolicy);
