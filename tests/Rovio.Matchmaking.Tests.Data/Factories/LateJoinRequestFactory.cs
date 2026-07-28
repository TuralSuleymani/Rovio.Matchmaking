using Rovio.Matchmaking.Application.Models;

namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class LateJoinRequestFactory
{
    public static LateJoinRequest Create(
        string? playerId = null,
        string? region = null,
        int? latencyMs = null) =>
        new(
            playerId ?? ThirdPlayerId,
            region ?? DefaultRegion,
            latencyMs ?? DefaultLatencyMs);
}
