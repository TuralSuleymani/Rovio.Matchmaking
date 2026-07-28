namespace Rovio.Matchmaking.Infrastructure.Redis;

public sealed class RedisTicketStore : ITicketStore
{
    private static readonly LuaScript EnqueueScript = LuaScript.Prepare(@"
        local playerKey = @playerKey
        local queueKey = @queueKey
        local maxDepth = tonumber(@maxDepth)
        local existing = redis.call('GET', playerKey)
        if existing then
          local ticketKey = 'mm:ticket:' .. existing
          local status = redis.call('HGET', ticketKey, 'status')
          if status == 'Queued' then
            return {0, existing}
          end
        end

        local depth = redis.call('ZCARD', queueKey)
        if depth >= maxDepth then
          return {-1, 'queue_full'}
        end

        local ticketId = @ticketId
        local ticketKey = 'mm:ticket:' .. ticketId
        redis.call('HSET', ticketKey,
          'ticketId', ticketId,
          'playerId', @playerId,
          'gameId', @gameId,
          'region', @region,
          'latencyMs', @latencyMs,
          'enqueuedAt', @enqueuedAt,
          'status', 'Queued')
        redis.call('SET', playerKey, ticketId)
        redis.call('ZADD', queueKey, tonumber(@score), ticketId)
        return {1, ticketId}
        ");

    private static readonly LuaScript CancelScript = LuaScript.Prepare(@"
        local ticketKey = @ticketKey
        local status = redis.call('HGET', ticketKey, 'status')
        if not status then
          return -1
        end
        if status ~= 'Queued' then
          return 0
        end
        local gameId = redis.call('HGET', ticketKey, 'gameId')
        local region = redis.call('HGET', ticketKey, 'region')
        local playerId = redis.call('HGET', ticketKey, 'playerId')
        local ticketId = redis.call('HGET', ticketKey, 'ticketId')
        redis.call('HSET', ticketKey, 'status', 'Cancelled')
        redis.call('ZREM', 'mm:queue:' .. gameId .. ':' .. region, ticketId)
        redis.call('DEL', 'mm:player:' .. gameId .. ':' .. playerId)
        return 1
        ");

    private readonly IConnectionMultiplexer _redis;

    public RedisTicketStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<Result<EnqueueResult, IDomainError>> EnqueueAsync(
        GameId gameId,
        PlayerId playerId,
        MatchRegion region,
        int latencyMs,
        int maxQueueDepth,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default)
    {
        var latencyResult = Latency.Create(latencyMs);
        if (latencyResult.IsFailure)
        {
            return Result.Failure<EnqueueResult, IDomainError>(latencyResult.Error);
        }

        var create = MatchTicket.CreateQueued(
            playerId,
            gameId,
            region,
            latencyResult.Value,
            enqueuedAt);
        if (create.IsFailure)
        {
            return Result.Failure<EnqueueResult, IDomainError>(create.Error);
        }

        var ticket = create.Value;

        try
        {
            var db = _redis.GetDatabase();
            var score = enqueuedAt.ToUnixTimeMilliseconds();

            var result = (RedisResult[])(await db.ScriptEvaluateAsync(EnqueueScript, new
            {
                playerKey = (RedisKey)RedisKeys.Player(ticket.GameId.Value, ticket.PlayerId.Value),
                queueKey = (RedisKey)RedisKeys.Queue(ticket.GameId.Value, ticket.Region.Value),
                maxDepth = maxQueueDepth,
                ticketId = ticket.Id.ToString(),
                playerId = ticket.PlayerId.Value,
                gameId = ticket.GameId.Value,
                region = ticket.Region.Value,
                latencyMs = ticket.Latency.Milliseconds,
                enqueuedAt = ticket.EnqueuedAt.ToString("O"),
                score
            }).WaitAsync(cancellationToken))!;

            var code = (int)result[0];
            if (code == -1)
            {
                return Result.Failure<EnqueueResult, IDomainError>(
                    DomainError.TooManyRequests(
                        $"Queue for {ticket.GameId}/{ticket.Region} is full.",
                        "queue_full"));
            }

            var returnedTicketId = (string)result[1]!;
            var created = code == 1;
            var ticketIdResult = Id<MatchTicket>.Create(returnedTicketId);
            if (ticketIdResult.IsFailure)
            {
                return Result.Failure<EnqueueResult, IDomainError>(
                    DomainError.Unexpected("Ticket id from enqueue was invalid.", "invalid_ticket_id"));
            }

            var loaded = await GetAsync(ticketIdResult.Value, cancellationToken);
            if (loaded.IsFailure)
            {
                return Result.Failure<EnqueueResult, IDomainError>(loaded.Error);
            }

            return new EnqueueResult(loaded.Value, created);
        }
        catch (Exception)
        {
            return Result.Failure<EnqueueResult, IDomainError>(
                DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable"));
        }
    }

    public async Task<Result<MatchTicket, IDomainError>> GetAsync(
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var entries = await db.HashGetAllAsync(RedisKeys.Ticket(ticketId.ToString())).WaitAsync(cancellationToken);
            if (entries.Length == 0)
            {
                return DomainError.NotFound($"Ticket '{ticketId}' was not found.", "ticket_not_found");
            }

            var map = entries.ToDictionary(x => (string)x.Name!, x => (string)x.Value!);
            return MapTicket(map);
        }
        catch (Exception)
        {
            return DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable");
        }
    }

    public async Task<UnitResult<IDomainError>> CancelAsync(
        GameId gameId,
        Id<MatchTicket> ticketId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var code = (int)await db.ScriptEvaluateAsync(CancelScript, new
            {
                ticketKey = (RedisKey)RedisKeys.Ticket(ticketId.ToString())
            }).WaitAsync(cancellationToken);

            return code switch
            {
                -1 => DomainError.NotFound($"Ticket '{ticketId}' was not found.", "ticket_not_found"),
                0 => DomainError.Conflict("Ticket is not in a cancellable queued state.", "not_queued"),
                1 => UnitResult.Success<IDomainError>(),
                _ => DomainError.Unexpected("Unexpected cancel result.", "redis_unexpected")
            };
        }
        catch (Exception)
        {
            return DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable");
        }
    }

    public async Task<Result<IReadOnlyList<MatchTicket>, IDomainError>> GetQueuedCandidatesAsync(
        GameId gameId,
        MatchRegion region,
        int limit,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var ids = await db.SortedSetRangeByRankAsync(RedisKeys.Queue(gameId.Value, region.Value), 0, limit - 1)
                .WaitAsync(cancellationToken);

            var tickets = new List<MatchTicket>();
            foreach (var id in ids)
            {
                var ticketIdResult = Id<MatchTicket>.Create((string?)id);
                if (ticketIdResult.IsFailure)
                {
                    continue;
                }

                var ticket = await GetAsync(ticketIdResult.Value, cancellationToken);
                if (ticket.IsSuccess && ticket.Value.Status == TicketStatus.Queued)
                {
                    tickets.Add(ticket.Value);
                }
            }

            return tickets;
        }
        catch (Exception)
        {
            return DomainError.Unavailable("Matchmaking runtime store is unavailable.", "redis_unavailable");
        }
    }

    private static MatchTicket MapTicket(IReadOnlyDictionary<string, string> map)
    {
        Id<GameSession>? sessionId = null;
        if (map.TryGetValue("sessionId", out var sid) && !string.IsNullOrEmpty(sid))
        {
            var sessionIdResult = Id<GameSession>.Create(sid);
            if (sessionIdResult.IsSuccess)
            {
                sessionId = sessionIdResult.Value;
            }
        }

        return MatchTicket.Rehydrate(
            Id<MatchTicket>.Create(map["ticketId"]).Value,
            PlayerId.Create(map["playerId"]).Value,
            GameId.Create(map["gameId"]).Value,
            MatchRegion.Create(map["region"]).Value,
            Latency.Create(int.Parse(map["latencyMs"], CultureInfo.InvariantCulture)).Value,
            DateTimeOffset.Parse(map["enqueuedAt"], CultureInfo.InvariantCulture),
            TicketStatus.FromName(map["status"]),
            sessionId).Value;
    }
}
