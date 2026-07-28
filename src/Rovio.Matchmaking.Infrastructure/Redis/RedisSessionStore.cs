namespace Rovio.Matchmaking.Infrastructure.Redis;

public sealed class RedisSessionStore : ISessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly LuaScript FormSessionScript = LuaScript.Prepare(@"
        local sessionId = @sessionId
        local ticketIds = cjson.decode(@ticketIdsJson)
        for _, ticketId in ipairs(ticketIds) do
          local ticketKey = 'mm:ticket:' .. ticketId
          local status = redis.call('HGET', ticketKey, 'status')
          if status ~= 'Queued' then
            return {-1, ticketId}
          end
        end

        for _, ticketId in ipairs(ticketIds) do
          local ticketKey = 'mm:ticket:' .. ticketId
          local gameId = redis.call('HGET', ticketKey, 'gameId')
          local region = redis.call('HGET', ticketKey, 'region')
          local playerId = redis.call('HGET', ticketKey, 'playerId')
          redis.call('HSET', ticketKey, 'status', 'Matched', 'sessionId', sessionId)
          redis.call('ZREM', 'mm:queue:' .. gameId .. ':' .. region, ticketId)
          redis.call('DEL', 'mm:player:' .. gameId .. ':' .. playerId)
        end

        redis.call('SET', 'mm:session:' .. sessionId, @sessionJson)
        if @allowLateJoin == '1' and @status ~= 'Full' then
          redis.call('SADD', 'mm:open:' .. @gameId .. ':' .. @region, sessionId)
        else
          redis.call('SREM', 'mm:open:' .. @gameId .. ':' .. @region, sessionId)
        end
        return {1, sessionId}
        ");

    private static readonly LuaScript LateJoinScript = LuaScript.Prepare(@"
        local sessionKey = 'mm:session:' .. @sessionId
        local sessionJson = redis.call('GET', sessionKey)
        if not sessionJson then
          return -1
        end

        local ticketKey = 'mm:ticket:' .. @ticketId
        local status = redis.call('HGET', ticketKey, 'status')
        if status ~= 'Queued' then
          return -2
        end

        local session = cjson.decode(sessionJson)
        if session.allowLateJoin ~= true then
          return -3
        end
        if #(session.playerIds) >= session.maxPlayers then
          return -4
        end

        table.insert(session.playerIds, @playerId)
        if #(session.playerIds) >= session.maxPlayers then
          session.status = 'Full'
          redis.call('SREM', 'mm:open:' .. @gameId .. ':' .. @region, @sessionId)
        else
          session.status = 'Formed'
          redis.call('SADD', 'mm:open:' .. @gameId .. ':' .. @region, @sessionId)
        end

        redis.call('SET', sessionKey, cjson.encode(session))
        redis.call('HSET', ticketKey, 'status', 'Matched', 'sessionId', @sessionId)
        redis.call('ZREM', 'mm:queue:' .. @gameId .. ':' .. @region, @ticketId)
        redis.call('DEL', 'mm:player:' .. @gameId .. ':' .. @playerId)
        return 1
        ");

    private readonly IConnectionMultiplexer _redis;

    public RedisSessionStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<Result<GameSession, IDomainError>> GetAsync(
        Id<GameSession> sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = await db.StringGetAsync(RedisKeys.Session(sessionId.ToString())).WaitAsync(cancellationToken);
            if (json.IsNullOrEmpty)
            {
                return DomainError.NotFound($"Session '{sessionId}' was not found.", "session_not_found");
            }

            return Deserialize((string)json!);
        }
        catch (Exception)
        {
            return DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable");
        }
    }

    public async Task<UnitResult<IDomainError>> FormSessionAsync(
        GameSession session,
        IReadOnlyList<Id<MatchTicket>> ticketIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var sessionJson = Serialize(session);
            var ticketIdsJson = JsonSerializer.Serialize(ticketIds.Select(t => t.ToString()));

            var result = (RedisResult[])(await db.ScriptEvaluateAsync(FormSessionScript, new
            {
                sessionId = session.Id.ToString(),
                ticketIdsJson,
                sessionJson,
                allowLateJoin = session.AllowLateJoin ? "1" : "0",
                status = session.Status.Name,
                gameId = session.GameId.Value,
                region = session.Region.Value
            }).WaitAsync(cancellationToken))!;

            var code = (int)result[0];
            if (code != 1)
            {
                return DomainError.Conflict(
                    $"Failed to claim tickets for session (conflict on '{(string)result[1]!}').",
                    "match_race");
            }

            return UnitResult.Success<IDomainError>();
        }
        catch (Exception)
        {
            return DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable");
        }
    }

    public async Task<Result<IReadOnlyList<GameSession>, IDomainError>> GetOpenSessionsAsync(
        GameId gameId,
        MatchRegion region,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var ids = await db.SetMembersAsync(RedisKeys.OpenSessions(gameId.Value, region.Value)).WaitAsync(cancellationToken);
            var sessions = new List<GameSession>();
            foreach (var idValue in ids)
            {
                var sessionIdResult = Id<GameSession>.Create((string?)idValue);
                if (sessionIdResult.IsFailure)
                {
                    continue;
                }

                var session = await GetAsync(sessionIdResult.Value, cancellationToken);
                if (session.IsSuccess && session.Value.CanLateJoin)
                {
                    sessions.Add(session.Value);
                }
            }

            return sessions;
        }
        catch (Exception)
        {
            return DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable");
        }
    }

    public async Task<UnitResult<IDomainError>> LateJoinAsync(
        Id<GameSession> sessionId,
        GameId gameId,
        MatchRegion region,
        Id<MatchTicket> ticketId,
        PlayerId playerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var code = (int)await db.ScriptEvaluateAsync(LateJoinScript, new
            {
                sessionId = sessionId.ToString(),
                ticketId = ticketId.ToString(),
                playerId = playerId.Value,
                gameId = gameId.Value,
                region = region.Value
            }).WaitAsync(cancellationToken);

            return code switch
            {
                -1 => DomainError.NotFound($"Session '{sessionId}' was not found.", "session_not_found"),
                -2 => DomainError.Conflict("Ticket is not queued.", "ticket_not_queued"),
                -3 => DomainError.Validation("Late join is disabled for this session.", code: "late_join_disabled"),
                -4 => DomainError.Conflict("Session has no open slots.", "session_full"),
                1 => UnitResult.Success<IDomainError>(),
                _ => DomainError.Unexpected("Unexpected late-join result.", "redis_unexpected")
            };
        }
        catch (Exception)
        {
            return DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable");
        }
    }

    private static string Serialize(GameSession session)
    {
        var payload = new SessionPayload(
            session.Id.ToString(),
            session.GameId.Value,
            session.Region.Value,
            session.Status.Name,
            session.PlayerCapacity.MinPlayerCount,
            session.PlayerCapacity.MaxPlayerCount,
            session.AllowLateJoin,
            session.PlayerIds.Select(p => p.Value).ToList(),
            session.CreatedAtUtc.ToString("O"),
            session.StartedAt.ToString("O"));
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static Result<GameSession, IDomainError> Deserialize(string json)
    {
        var payload = JsonSerializer.Deserialize<SessionPayload>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid session JSON.");

        var capacity = PlayerCapacity.Create(payload.MinPlayers, payload.MaxPlayers).Value;
        var players = payload.PlayerIds.Select(p => PlayerId.Create(p).Value).ToList();
        var createdAt = DateTimeOffset.Parse(payload.CreatedAt, CultureInfo.InvariantCulture);
        var startedAt = string.IsNullOrEmpty(payload.StartedAt)
            ? createdAt
            : DateTimeOffset.Parse(payload.StartedAt, CultureInfo.InvariantCulture);

        var statusName = string.Equals(payload.Status, "InProgress", StringComparison.Ordinal)
            ? SessionStatus.Formed.Name
            : payload.Status;

        var rehydrated = GameSession.Rehydrate(
            Id<GameSession>.Create(payload.SessionId).Value,
            GameId.Create(payload.GameId).Value,
            MatchRegion.Create(payload.Region).Value,
            capacity,
            SessionStatus.FromName(statusName),
            payload.AllowLateJoin,
            players,
            createdAt,
            startedAt);

        if (rehydrated.IsFailure)
        {
            return DomainError.Unexpected(
                rehydrated.Error.ErrorMessage ?? "Invalid persisted session.",
                rehydrated.Error.Code ?? "invalid_session");
        }

        return rehydrated.Value;
    }

    private sealed record SessionPayload(
        string SessionId,
        string GameId,
        string Region,
        string Status,
        int MinPlayers,
        int MaxPlayers,
        bool AllowLateJoin,
        List<string> PlayerIds,
        string CreatedAt,
        string? StartedAt);
}
