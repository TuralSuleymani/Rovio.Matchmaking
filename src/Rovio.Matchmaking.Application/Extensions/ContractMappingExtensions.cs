namespace Rovio.Matchmaking.Application.Extensions;

public static class ContractMappingExtensions
{
    public static GameMatchConfigDto ToDto(this GameMatchConfig config) => new(
        config.GameId.Value,
        config.PlayerCapacity.MinPlayerCount,
        config.PlayerCapacity.MaxPlayerCount,
        config.AllowLateJoin,
        config.Enabled,
        config.MaxQueueDepth,
        new LatencyPolicyDto(
            config.LatencyPolicy.BaseMaximumDelta.Milliseconds,
            (int)config.LatencyPolicy.ExpansionInterval.TotalSeconds,
            config.LatencyPolicy.ExpansionStep.Milliseconds,
            config.LatencyPolicy.AbsoluteMaximumDelta.Milliseconds),
        config.CreatedAt,
        config.UpdatedAt);

    public static TicketDto ToDto(this MatchTicket ticket) => new(
        ticket.Id.ToString(),
        ticket.PlayerId.Value,
        ticket.GameId.Value,
        ticket.Region.Value,
        ticket.Latency.Milliseconds,
        ticket.EnqueuedAt,
        ticket.Status.Name,
        ticket.SessionId?.ToString());

    public static SessionDto ToDto(this GameSession session) => new(
        session.Id.ToString(),
        session.GameId.Value,
        session.Region.Value,
        session.Status.Name,
        session.MaxPlayers,
        session.AllowLateJoin,
        session.PlayerIds.Select(p => p.Value).ToList(),
        session.CreatedAtUtc,
        session.StartedAt);
}
