using Rovio.Matchmaking.Application.Models;

namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class EnqueueRequestFactory
{
    public static EnqueueRequest Create(
        string? playerId = null,
        string? region = null,
        int? latencyMs = null) =>
        new(
            playerId ?? DefaultPlayerId,
            region ?? DefaultRegion,
            latencyMs ?? DefaultLatencyMs);
}
