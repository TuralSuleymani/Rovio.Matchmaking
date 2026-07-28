namespace Rovio.Matchmaking.Domain.Errors;

public static class DomainErrorCodes
{
    public const string InvalidGameId = "invalid_game_id";
    public const string InvalidPlayerId = "invalid_player_id";
    public const string InvalidMatchRegion = "invalid_match_region";
    public const string InvalidLatency = "invalid_latency";
    public const string InvalidLatencyDelta = "invalid_latency_delta";
    public const string InvalidLatencyDeltaMultiplier = "invalid_latency_delta_multiplier";
    public const string LatencyDeltaOverflow = "latency_delta_overflow";
    public const string InvalidLatencyPolicy = "invalid_latency_policy";
    public const string InvalidMatchSize = "invalid_match_size";
    public const string InvalidConfig = "invalid_config";
    public const string InvalidSession = "invalid_session";
    public const string InvalidRegion = "invalid_region";
    public const string InsufficientPlayers = "insufficient_players";
    public const string SessionFull = "session_full";
    public const string PlayerCapacityExceeded = "player_capacity_exceeded";
    public const string DuplicateSessionPlayer = "duplicate_session_player";
    public const string LateJoinDisabled = "late_join_disabled";
    public const string InvalidTicketTransition = "invalid_ticket_transition";
    public const string InvalidTicketState = "invalid_ticket_state";
    public const string InvalidMatchingInput = "invalid_matching_input";
    public const string MismatchedMatchShard = "mismatched_match_shard";
    public const string DuplicateQueuedPlayer = "duplicate_queued_player";
}
