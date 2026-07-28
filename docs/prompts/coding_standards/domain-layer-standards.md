# Domain Layer Standards

This matchmaking solution is modeled as a single **bounded context**: players enqueue, get matched by latency and wait time, then may late-join an open session. DDD-inspired building blocks keep those rules in one place so the API and Worker cannot drift into conflicting “business logic” copies.

The Domain layer owns **meaning and invariants** only. It must not reference Redis keys, HTTP DTOs, EF entities, or infrastructure packages.

## Why DDD here

Matchmaking rules such as `LatencyPolicy`, `PlayerCapacity`, and ticket status transitions must stay consistent across enqueue, cancel, match formation, and late join. Putting them in a domain model—rather than scattering `string`/`int` checks in controllers and Redis adapters—avoids **duplicate validation** and **primitive obsession**.

## Entities

Entities have **identity and lifecycle**. We use three:

- **`GameMatchConfig`** - per-game policy (capacity, late join, latency policy, queue depth, enabled).
- **`MatchTicket`** - a player’s queued request until matched or cancelled (`MarkMatched` / `Cancel` enforce legal transitions).
- **`GameSession`** - the formed lobby (`TryAddPlayer` / `EnsureCanLateJoin` protect capacity and late-join rules).

Without entity methods, the same status flags and slot checks would be reimplemented in the Worker, API, and Lua-adjacent application code-easy to get wrong twice.

## Value objects

Value objects are **immutable, validated, and without identity**: `GameId`, `PlayerId`, `MatchRegion`, `Latency`, `LatencyDelta`, `LatencyPolicy`, `PlayerCapacity`.

Prefer typed boundaries over primitives. A signature like `Enqueue(string playerId, string region, int latencyMs)` invites swapped arguments and repeats trim/length/range checks everywhere. `PlayerId.Create`, `MatchRegion.Create`, and `Latency.Create` fail **once** at the edge; the rest of the domain can trust the types.

`PlayerCapacity` owns min/max party rules so config, session formation, and matching do not each invent `minPlayers`/`maxPlayers` integers. `LatencyPolicy.MaximumAcceptableDelta(wait)` encapsulates wait-expanded tolerance instead of copying expansion math in `MatchingService` and somewhere else later.

Equality follows value: two `PlayerId`s with the same normalized string are the same player id concept, unlike two `MatchTicket`s with different ids.

## Domain services

Use a domain service when a rule **spans multiple entities**. `MatchingService.SelectMatchGroup(candidates, config, region, now)` chooses a fair, pairwise latency-compatible group for one `{game, region}` shard-that decision does not belong on `MatchTicket` or `GameSession` alone.

Centralizing oldest-first fairness and compatibility in `MatchingService` prevents shotgun surgery: changing match policy should not require parallel edits in the API and Worker.

## Results and boundaries

Factories and transitions return `Result` / `DomainError` for expected failures so invalid aggregates are not constructed silently. Parse primitives into value objects at the application/API boundary; inside Domain prefer the typed forms.

**Do not** leak persistence or transport shapes into Domain, and **do not** reintroduce bare `string` ids/regions in domain methods after they have been parsed.
