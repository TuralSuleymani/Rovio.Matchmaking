namespace Rovio.Matchmaking.Application.Models.Dtos;

public sealed record LatencyPolicyDto(
    int BaseMaxLatencyDeltaMs,
    int ExpansionIntervalSeconds,
    int ExpansionStepMs,
    int AbsoluteMaxLatencyDeltaMs);
