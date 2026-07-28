namespace Rovio.Matchmaking.Infrastructure.Redis;

public sealed record GameMatchConfigProjection(
    string GameId,
    int MinPlayers,
    int MaxPlayers,
    bool AllowLateJoin,
    bool Enabled,
    int? MaxQueueDepth,
    int BaseMaxLatencyDeltaMs,
    int ExpansionIntervalSeconds,
    int ExpansionStepMs,
    int AbsoluteMaxLatencyDeltaMs,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static GameMatchConfigProjection FromDomain(GameMatchConfig config) => new(
        config.GameId.Value,
        config.PlayerCapacity.MinPlayerCount,
        config.PlayerCapacity.MaxPlayerCount,
        config.AllowLateJoin,
        config.Enabled,
        config.MaxQueueDepth,
        config.LatencyPolicy.BaseMaximumDelta.Milliseconds,
        (int)config.LatencyPolicy.ExpansionInterval.TotalSeconds,
        config.LatencyPolicy.ExpansionStep.Milliseconds,
        config.LatencyPolicy.AbsoluteMaximumDelta.Milliseconds,
        config.CreatedAt,
        config.UpdatedAt);

    public GameMatchConfig ToDomain()
    {
        var latency = LatencyPolicy.Create(
            BaseMaxLatencyDeltaMs,
            ExpansionIntervalSeconds,
            ExpansionStepMs,
            AbsoluteMaxLatencyDeltaMs).Value;

        return GameMatchConfig.Create(
            GameId,
            MinPlayers,
            MaxPlayers,
            AllowLateJoin,
            Enabled,
            MaxQueueDepth,
            latency,
            CreatedAt,
            UpdatedAt).Value;
    }
}
