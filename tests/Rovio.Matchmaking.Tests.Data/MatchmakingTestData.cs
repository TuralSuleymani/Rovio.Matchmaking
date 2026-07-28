using Rovio.Domain.Common.Errors;
using DomainCodes = Rovio.Matchmaking.Domain.Errors.DomainErrorCodes;

namespace Rovio.Matchmaking.Tests.Data;

public static class MatchmakingTestData
{
    public static class ErrorCodes
    {
        public const string InvalidId = CommonErrorCodes.InvalidId;
        public const string InvalidGameId = DomainCodes.InvalidGameId;
        public const string InvalidPlayerId = DomainCodes.InvalidPlayerId;
        public const string InvalidMatchRegion = DomainCodes.InvalidMatchRegion;
        public const string InvalidLatency = DomainCodes.InvalidLatency;
        public const string InvalidLatencyDelta = DomainCodes.InvalidLatencyDelta;
        public const string InvalidLatencyDeltaMultiplier = DomainCodes.InvalidLatencyDeltaMultiplier;
        public const string LatencyDeltaOverflow = DomainCodes.LatencyDeltaOverflow;
        public const string InvalidLatencyPolicy = DomainCodes.InvalidLatencyPolicy;
        public const string InvalidMatchSize = DomainCodes.InvalidMatchSize;
        public const string InvalidConfig = DomainCodes.InvalidConfig;
        public const string InvalidSession = DomainCodes.InvalidSession;
        public const string InvalidRegion = DomainCodes.InvalidRegion;
        public const string InsufficientPlayers = DomainCodes.InsufficientPlayers;
        public const string SessionFull = DomainCodes.SessionFull;
        public const string PlayerCapacityExceeded = DomainCodes.PlayerCapacityExceeded;
        public const string DuplicateSessionPlayer = DomainCodes.DuplicateSessionPlayer;
        public const string LateJoinDisabled = DomainCodes.LateJoinDisabled;
        public const string InvalidTicketTransition = DomainCodes.InvalidTicketTransition;
        public const string InvalidTicketState = DomainCodes.InvalidTicketState;
        public const string InvalidMatchingInput = DomainCodes.InvalidMatchingInput;
        public const string MismatchedMatchShard = DomainCodes.MismatchedMatchShard;
        public const string DuplicateQueuedPlayer = DomainCodes.DuplicateQueuedPlayer;

        public const string GameNotFound = "game_not_found";
        public const string PostgresUnavailable = "postgres_unavailable";
        public const string ConfigProjectionFailed = "config_projection_failed";
        public const string ConfigBootstrapFailed = "config_bootstrap_failed";
        public const string RedisUnavailable = "redis_unavailable";
        public const string GameDisabled = "game_disabled";
        public const string InvalidTicketId = "invalid_ticket_id";
        public const string TicketNotFound = "ticket_not_found";
        public const string AlreadyMatched = "already_matched";
        public const string AlreadyCancelled = "already_cancelled";
        public const string InvalidSessionId = "invalid_session_id";
        public const string RegionMismatch = "region_mismatch";
        public const string TicketNotQueued = "ticket_not_queued";
        public const string NotQueued = "not_queued";
        public const string QueueFull = "queue_full";
        public const string SessionNotFound = "session_not_found";
        public const string MatchRace = "match_race";
    }

    public static readonly Guid EmptyGuid = Guid.Empty;
    public static readonly Guid SampleGuid =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string SampleGuidString = "11111111-1111-1111-1111-111111111111";
    public const string EmptyGuidString = "00000000-0000-0000-0000-000000000000";
    public const string InvalidIdString = "not-a-guid";
    public const string IdWithWhitespace = "  11111111-1111-1111-1111-111111111111  ";

    public const string AngryBirds2GameId = "angry-birds-2";
    public const string AlternateGameId = "angry-birds-friends";
    public const string GameIdWithWhitespace = "  angry-birds-2  ";
    public static readonly string TooLongGameId =
        new string('g', GameId.MaximumLength + 1);

    public const string DefaultPlayerId = "player-1";
    public const string SecondPlayerId = "player-2";
    public const string ThirdPlayerId = "player-3";
    public const string FourthPlayerId = "player-4";
    public const string FifthPlayerId = "player-5";
    public const string NewPlayerId = "p-new";
    public const string OlderPlayerId1 = "p-old1";
    public const string OlderPlayerId2 = "p-old2";
    public const string PlayerIdWithWhitespace = "  player-trimmed  ";
    public const string TrimmedPlayerId = "player-trimmed";

    public const string DefaultRegion = "eu";
    public const string RegionWithWhitespace = "  EU  ";
    public const string NormalizedRegion = "eu";
    public const string NaRegion = "na";
    public static readonly string TooLongRegion =
        new string('r', MatchRegion.MaximumLength + 1);

    public const string EmptyString = "";
    public const string WhitespaceString = "   ";
    public const string GuidNFormat = "N";

    public const int DefaultLatencyMs = 40;
    public const int CompatibleLatencyMs = 45;
    public const int CompatibleLatencyOffsetMs = 3;
    public const int IncompatibleLatencyMs = 200;
    public const int ZeroLatencyMs = 0;
    public const int NegativeLatencyMs = -1;

    public const int DefaultLatencyDeltaMs = 50;
    public const int SmallLatencyDeltaMs = 25;
    public const int LargeLatencyDeltaMs = 100;
    public const int AbsoluteMaxLatencyDeltaMs = 100;
    public const int NegativeLatencyDeltaMs = -1;
    public const int BaseMaxAboveAbsoluteBaseMs = 100;
    public const int BaseMaxAboveAbsoluteAbsoluteMs = 50;
    public const int ZeroExpansionStepMs = 0;
    public const int LatencyDeltaMultiplier = 3;
    public const int NegativeLatencyDeltaMultiplier = -2;
    public const int OverflowLatencyDeltaMultiplier = 2;
    public const int DefaultExpansionIntervalSeconds = 10;
    public const int DefaultExpansionStepMs = 25;
    public const int ZeroExpansionIntervalSeconds = 0;
    public const int NegativeExpansionIntervalSeconds = -5;

    public const int ExpectedExpandedLatencyDeltaMs =
        DefaultLatencyDeltaMs + DefaultExpansionStepMs;
    public const int ExpectedCombinedLatencyDeltaMs =
        SmallLatencyDeltaMs + DefaultLatencyDeltaMs;
    public const int ExpectedScaledLatencyDeltaMs =
        SmallLatencyDeltaMs * LatencyDeltaMultiplier;
    public const int ExpectedLatencyDifferenceMs =
        IncompatibleLatencyMs - DefaultLatencyMs;

    public const int DefaultMinPlayers = 2;
    public const int DefaultMaxPlayers = 4;
    public const int DuoMaxPlayers = 2;
    public const int TrioMaxPlayers = 3;
    public const int InvalidMinPlayers = 1;
    public const int ZeroMinPlayers = 0;
    public const int LargeSquadMinPlayers = 3;
    public const int LargeSquadMaxPlayers = 8;
    public const int InvertedCapacityMinPlayers = 4;
    public const int InvertedCapacityMaxPlayers = 2;
    public const int BelowMinimumPlayerCount = 1;
    public const int MidCapacityPlayerCount = 3;
    public const int AboveMaximumPlayerCount = 5;

    public const int ValidMaxQueueDepth = 100;
    public const int InvalidMaxQueueDepth = 0;
    public const int NegativeMaxQueueDepth = -1;
    public const int DefaultOptionsMaxQueueDepth = 10_000;

    public static readonly DateTimeOffset DefaultNow =
        DateTimeOffset.Parse("2026-01-01T00:01:00Z");

    public static readonly DateTimeOffset RecentEnqueueAt =
        DefaultNow.AddSeconds(-5);

    public static readonly DateTimeOffset OlderEnqueueAt =
        DefaultNow.AddSeconds(-60);

    public static readonly DateTimeOffset OldestEnqueueAt =
        DefaultNow.AddSeconds(-120);

    public static readonly DateTimeOffset OldestEnqueueAtWithOffset =
        OldestEnqueueAt.AddSeconds(70);

    public static readonly TimeSpan ZeroWait = TimeSpan.Zero;
    public static readonly TimeSpan PartialExpansionInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan OneExpansionInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan ExactCapExpansionWait = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan LongWait = TimeSpan.FromSeconds(100);
}
