namespace Rovio.Matchmaking.Application.Options;

public sealed class MatchmakingOptions
{
    public const string SectionName = "Matchmaking";

    public int WorkerIntervalMs { get; set; } = 500;
    public int ShardLockSeconds { get; set; } = 5;
    public int MaxQueueDepth { get; set; } = 10_000;
    public int MatchCandidateLimit { get; set; } = 200;
    public string[] SeedRegions { get; set; } = ["eu", "na", "asia"];
}
