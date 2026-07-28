using Rovio.Matchmaking.Application.Models;
using Rovio.Matchmaking.Application.Models.Dtos;

namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class UpsertGameConfigRequestFactory
{
    public static UpsertGameConfigRequest Create(
        int? minPlayers = null,
        int? maxPlayers = null,
        bool allowLateJoin = true,
        bool enabled = true,
        int? maxQueueDepth = ValidMaxQueueDepth,
        LatencyPolicyDto? latencyPolicy = null) =>
        new(
            minPlayers ?? DefaultMinPlayers,
            maxPlayers ?? DefaultMaxPlayers,
            allowLateJoin,
            enabled,
            maxQueueDepth,
            latencyPolicy);

    public static UpsertGameConfigRequest CreateWithLatencyPolicy(
        int? baseMaxLatencyDeltaMs = null,
        int? expansionIntervalSeconds = null,
        int? expansionStepMs = null,
        int? absoluteMaxLatencyDeltaMs = null) =>
        Create(
            latencyPolicy: new LatencyPolicyDto(
                baseMaxLatencyDeltaMs ?? DefaultLatencyDeltaMs,
                expansionIntervalSeconds ?? DefaultExpansionIntervalSeconds,
                expansionStepMs ?? DefaultExpansionStepMs,
                absoluteMaxLatencyDeltaMs ?? AbsoluteMaxLatencyDeltaMs));

    public static UpsertGameConfigRequest CreateWithInvalidLatencyPolicy() =>
        Create(
            latencyPolicy: new LatencyPolicyDto(
                BaseMaxAboveAbsoluteBaseMs,
                DefaultExpansionIntervalSeconds,
                DefaultExpansionStepMs,
                BaseMaxAboveAbsoluteAbsoluteMs));

    public static UpsertGameConfigRequest CreateWithInvalidMatchSize() =>
        Create(
            minPlayers: InvalidMinPlayers,
            maxPlayers: DefaultMaxPlayers);
}
