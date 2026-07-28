
namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class MatchTicketFactory
{
    public static MatchTicket CreateQueued(
        string? playerId = null,
        string? gameId = null,
        string? region = null,
        int? latencyMs = null,
        DateTimeOffset? enqueuedAt = null,
        Id<MatchTicket>? id = null) =>
        MatchTicket.CreateQueued(
            playerId ?? DefaultPlayerId,
            gameId ?? AngryBirds2GameId,
            region ?? DefaultRegion,
            latencyMs ?? DefaultLatencyMs,
            enqueuedAt ?? OlderEnqueueAt,
            id).Value;

    public static MatchTicket CreateQueued(
        PlayerId playerId,
        GameId? gameId = null,
        MatchRegion? region = null,
        Latency? latency = null,
        DateTimeOffset? enqueuedAt = null) =>
        MatchTicket.CreateQueued(
            playerId,
            gameId ?? GameIdFactory.Create(),
            region ?? MatchRegionFactory.Create(),
            latency ?? LatencyFactory.Create(),
            enqueuedAt ?? OlderEnqueueAt).Value;

    public static MatchTicket RehydrateMatched(
        Id<GameSession> sessionId,
        string? playerId = null,
        DateTimeOffset? enqueuedAt = null,
        Id<MatchTicket>? id = null) =>
        MatchTicket.Rehydrate(
            id ?? Id<MatchTicket>.New(),
            PlayerIdFactory.Create(playerId),
            GameIdFactory.Create(),
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            enqueuedAt ?? OlderEnqueueAt,
            TicketStatus.Matched,
            sessionId).Value;

    public static MatchTicket RehydrateCancelled(
        string? playerId = null,
        string? gameId = null,
        Id<MatchTicket>? id = null) =>
        MatchTicket.Rehydrate(
            id ?? Id<MatchTicket>.New(),
            PlayerIdFactory.Create(playerId),
            GameIdFactory.Create(gameId),
            MatchRegionFactory.Create(),
            LatencyFactory.Create(),
            OlderEnqueueAt,
            TicketStatus.Cancelled,
            sessionId: null).Value;
}
