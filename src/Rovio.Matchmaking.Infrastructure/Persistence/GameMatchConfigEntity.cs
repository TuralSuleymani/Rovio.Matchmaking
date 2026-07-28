namespace Rovio.Matchmaking.Infrastructure.Persistence;

public sealed class GameMatchConfigEntity
{
    public string GameId { get; set; } = string.Empty;
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public bool AllowLateJoin { get; set; }
    public bool Enabled { get; set; }
    public int? MaxQueueDepth { get; set; }
    public string LatencyPolicyJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GameMatchConfig ToDomain()
    {
        var doc = JsonSerializer.Deserialize<LatencyPolicyDocument>(LatencyPolicyJson)
            ?? LatencyPolicyDocument.Default;
        var latency = LatencyPolicy.Create(
            doc.BaseMaxLatencyDeltaMs,
            doc.ExpansionIntervalSeconds,
            doc.ExpansionStepMs,
            doc.AbsoluteMaxLatencyDeltaMs).Value;

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

    public static GameMatchConfigEntity FromDomain(GameMatchConfig config) => new()
    {
        GameId = config.GameId.Value,
        MinPlayers = config.PlayerCapacity.MinPlayerCount,
        MaxPlayers = config.PlayerCapacity.MaxPlayerCount,
        AllowLateJoin = config.AllowLateJoin,
        Enabled = config.Enabled,
        MaxQueueDepth = config.MaxQueueDepth,
        LatencyPolicyJson = JsonSerializer.Serialize(LatencyPolicyDocument.From(config.LatencyPolicy)),
        CreatedAt = config.CreatedAt,
        UpdatedAt = config.UpdatedAt
    };

    private sealed record LatencyPolicyDocument(
        int BaseMaxLatencyDeltaMs,
        int ExpansionIntervalSeconds,
        int ExpansionStepMs,
        int AbsoluteMaxLatencyDeltaMs)
    {
        public static LatencyPolicyDocument Default { get; } = new(
            LatencyPolicy.DefaultBaseMaxLatencyDeltaMs,
            LatencyPolicy.DefaultExpansionIntervalSeconds,
            LatencyPolicy.DefaultExpansionStepMs,
            LatencyPolicy.DefaultAbsoluteMaxLatencyDeltaMs);

        public static LatencyPolicyDocument From(LatencyPolicy policy) => new(
            policy.BaseMaximumDelta.Milliseconds,
            (int)policy.ExpansionInterval.TotalSeconds,
            policy.ExpansionStep.Milliseconds,
            policy.AbsoluteMaximumDelta.Milliseconds);
    }
}
