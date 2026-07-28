This document reflects my practical experience with integration testing and the approach I follow in my day-to-day development work. I adopted these standards and practices several years ago and have continued to refine them over time.

They are not intended to represent a perfect or universally applicable set of rules. Instead, they provide a consistent and practical foundation for writing readable, maintainable, and reliable unit tests. These guidelines may continue to evolve as new lessons are learned, project requirements change, and better approaches emerge. Only examples were adapted to the current project.

# Integration Testing Standards

These standards apply to integration tests in the Infrastructure and API layers. Tests must be readable, deterministic, focused on observable behavior through **real adapters**, and independent of local hand-configured databases.

They pair with the unit-testing standards: unit tests mock ports and never touch Docker; integration tests use Testcontainers and production DI.

## Purpose and scope

These standards apply to:

- `tests/Rovio.Matchmaking.Infrastructure.Tests.Integration`
- `tests/Rovio.Matchmaking.Api.Tests.Integration`

Shared support lives in `tests/Rovio.Matchmaking.Tests.Data` (factories, `MatchmakingTestData`, `FixedTimeProvider`). That project is **not** a test runner.

Integration tests verify behavior that unit tests cannot: Redis Lua and store semantics, EF persistence, HTTP contracts, and the matchmaking engine against real stores.

## When to write an integration test vs a unit test

| Prefer unit | Prefer integration |
|---|---|
| Domain rules and value-object invariants | Redis queues, tickets, sessions, locks, Lua |
| Application services with substituted ports | Postgres config repository / migrations |
| Pure orchestration and Result mapping | Full HTTP pipeline (`WebApplicationFactory`) |
| | `IMatchmakingEngine` forming sessions against real Redis |

Anything that **requires real infrastructure** belongs in an integration project, not in the unit-test suite.

## Test naming convention

Use the same format as unit tests:

```text
MethodName_WhenCondition_ShouldExpectedResult
```

Examples:

```csharp
EnqueueAsync_WhenNewPlayer_ShouldCreateQueuedTicket()

EnqueueAsync_WhenQueueFull_ShouldFail()

RunOnceAsync_WhenCompatibleQueuedPlayers_ShouldFormSession()

Enqueue_WhenNewPlayer_ShouldCreateTicket()

Enqueue_WhenQueueFull_ShouldReturn429()
```

Include the `Async` suffix when it is part of the production method name.

For API tests, the “method” may be the **HTTP behavior** (`Enqueue_...`, `Ready_...`) when the surface under test is an endpoint rather than a C# type member.

## Use Arrange / Act / Assert

Tests must clearly separate setup, execution, and verification.

Call `RequireDocker()` at the start of every test as a **precondition** (before Arrange). It fails fast with a clear message when containers or the host did not start.

```csharp
[Fact]
public async Task EnqueueAsync_WhenNewPlayer_ShouldCreateQueuedTicket()
{
    RequireDocker();

    // Arrange
    var gameId = UniqueGameId("tq");
    var playerId = UniquePlayerId();
    var region = UniqueRegion();
    var store = Resolve<ITicketStore>();

    // Act
    var result = await store.EnqueueAsync(
        gameId,
        playerId,
        region,
        DefaultLatencyMs,
        ValidMaxQueueDepth,
        OlderEnqueueAt);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Created.Should().BeTrue();
    result.Value.Ticket.Status.Should().Be(TicketStatus.Queued);
}
```

For non-trivial multi-step flows (match formation, late join), keep the `Arrange`, `Act`, and `Assert` comments so the flow stays visible.

## Two integration suites

| | Infrastructure suite | API suite |
|---|---|---|
| **Project** | `Rovio.Matchmaking.Infrastructure.Tests.Integration` | `Rovio.Matchmaking.Api.Tests.Integration` |
| **Proves** | Application ports against real Redis/Postgres | HTTP contracts through the real ASP.NET host |
| **Entry** | `ServiceCollection` + `AddMatchmakingInfrastructure` | `WebApplicationFactory<Program>` + `HttpClient` |
| **IDs** | Domain value objects (`GameId`, `PlayerId`, `MatchRegion`) | Strings (API path / body values) |
| **Time** | `FixedTimeProvider` replaces `TimeProvider` | Wall clock (no fixed clock) |
| **Base class** | `BaseInfrastructureSpec` | `BaseApiSpec` |
| **Collection** | `"Infrastructure"` | `"Api"` |

### Infrastructure suite

Resolve ports such as `ITicketStore`, `ISessionStore`, `IShardLock`, `IGameConfigRepository`, `IGameConfigProjector`, `IGameConfigRuntime`, and `IMatchmakingEngine`. Assert `Result` outcomes and persisted round-trips.

### API suite

Drive the public REST API. Assert `HttpStatusCode`, response DTOs, and follow-up GETs. When a scenario needs matching, call `RunMatchOnceAsync()` explicitly - do not wait for the Worker host timer.

## Dockerization and Testcontainers

Docker **must** be running on the machine that executes integration tests.

Standards:

- Use **Testcontainers** images consistent with this repo: `postgres:16-alpine` and `redis:7-alpine`.
- Start containers **once per collection** inside the fixture’s `InitializeAsync` (start Postgres and Redis in parallel).
- Inject connection strings through configuration (in-memory overrides or `WebApplicationFactory.UseSetting`).
- Dispose containers in fixture teardown after the host / `ServiceProvider`.
- On startup failure, set `DockerAvailable = false` and capture `StartupError`. Tests must call `RequireDocker()` and fail with that message — **do not silently skip**.
- Packages: `Testcontainers.PostgreSql`, `Testcontainers.Redis`.

```csharp
protected void RequireDocker()
{
    if (!Fixture.DockerAvailable)
    {
        throw new InvalidOperationException(
            $"Docker/Testcontainers unavailable: {Fixture.StartupError}");
    }
}
```

## Shared collection fixtures

Use one xUnit collection and one shared fixture per suite:

```csharp
[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<InfrastructureFixture>
{
    public const string Name = "Infrastructure";
}
```

```csharp
[Collection(InfrastructureCollection.Name)]
public abstract class BaseInfrastructureSpec
{
    protected BaseInfrastructureSpec(InfrastructureFixture fixture) => Fixture = fixture;
    // ...
}
```

Test classes inherit the base and take the fixture via constructor injection:

```csharp
public sealed class RedisTicketStoreTests(InfrastructureFixture fixture)
    : BaseInfrastructureSpec(fixture)
{
}
```

Do **not** start containers or rebuild the DI host inside individual tests.

## Real vs fake

### Must be real

- Postgres (including EF migrations)
- Redis (stores, Lua scripts, shard locks, config projection)
- Production DI registration (`AddMatchmakingInfrastructure` or the API `Program` host)
- ASP.NET Core pipeline in the API suite (`WebApplicationFactory<Program>`)

### Allowed fakes and test controls

| Control | Where | Why |
|---|---|---|
| `FixedTimeProvider` replacing `TimeProvider` | Infrastructure | Deterministic timestamps |
| In-memory configuration | Both | Connection strings, `MaxQueueDepth`, seed regions |
| Explicit `IMatchmakingEngine.RunOnceAsync()` | Both (when matching) | Deterministic match ticks without Worker timers |
| Unique ID helpers | Both | Isolation on shared containers |

### Do not

- Substitute `ITicketStore`, `ISessionStore`, Redis, or Postgres in integration tests.
- Replace `IMatchmakingEngine` with a stub.
- Use NSubstitute for application ports in these projects (that belongs in unit tests).

## Isolation and cleanup

Containers and data stores are **shared for the collection lifetime**. Isolation is primarily by **unique keys**, not by wiping the world every test.

Standards:

- Prefer `UniqueGameId` / `UniquePlayerId` / `UniqueRegion` from the base spec so Redis shards and Postgres rows do not collide.
- Migrate Postgres **once** at fixture initialization.
- Call `ClearPostgresAsync` (truncate) only when the assertion depends on empty or global list state.
- Prefer unique Redis keys over flushing every test. Flush helpers may exist for emergency or suite-level reset; do not make per-test flush the default.
- Do not assume leftover data from another test is absent unless you truncated or used unique IDs.

```csharp
RequireDocker();
var gameId = UniqueGameId("eng");
var region = UniqueRegion();
```

## DI resolution (Infrastructure)

- Use root `Resolve<T>()` for ports registered as singletons (most Redis adapters).
- Use `Fixture.CreateScope()` and `GetRequiredService<T>()` for **scoped** services (EF repositories, `IMatchmakingEngine`).
- Build the test `ServiceProvider` with `validateScopes: true`.

```csharp
private async Task RunEngineOnceAsync()
{
    await using var scope = Fixture.CreateScope();
    var engine = scope.ServiceProvider.GetRequiredService<IMatchmakingEngine>();
    await engine.RunOnceAsync();
}
```

## HTTP helpers (API)

Use helpers on `BaseApiSpec` instead of duplicating URLs and JSON:

- `EnsureGameConfigAsync` / `PutConfigAsync`
- `EnqueueAsync` / `EnqueueSuccessAsync`
- `GetTicketAsync`
- `RunMatchOnceAsync`

Assert status codes and DTO fields with FluentAssertions:

```csharp
[Fact]
public async Task Enqueue_WhenNewPlayer_ShouldCreateTicket()
{
    RequireDocker();
    var gameId = UniqueGameId("enq");
    var playerId = UniquePlayerId();
    var region = UniqueRegion();
    await EnsureGameConfigAsync(gameId);

    var (response, ticket) = await EnqueueAsync(
        gameId,
        playerId: playerId,
        region: region,
        latencyMs: DefaultLatencyMs);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
    ticket.Should().NotBeNull();
    ticket!.Status.Should().Be(TicketStatus.Queued.Name);
}
```

## Prefer Result and HTTP outcomes for expected failures

### Infrastructure

When a port returns `Result` / `UnitResult`, assert business failures through that result (same rule as unit tests):

```csharp
result.IsFailure.Should().BeTrue();
result.Error.Code.Should().Be(ErrorCodes.QueueFull);
```

### API

Assert the HTTP contract the client sees (`429`, `404`, `409`, Problem Details body fields as needed). Do not assert internal exceptions for expected business failures.

Exception assertions are appropriate only for unexpected infrastructure or programming errors that are not translated into a Result or Problem Details response.

## Use test factories and shared defaults

Use `Rovio.Matchmaking.Tests.Data` factories and `MatchmakingTestData` constants. Avoid magic values.

```csharp
await store.EnqueueAsync(
    gameId,
    playerId,
    region,
    DefaultLatencyMs,
    ValidMaxQueueDepth,
    OlderEnqueueAt);
```

```csharp
await EnsureGameConfigAsync(gameId, maxPlayers: DuoMaxPlayers);
var request = EnqueueRequestFactory.Create(playerId: playerId, region: region);
```

- Domain entity factories for Infrastructure scenarios (`GameMatchConfigFactory`, `MatchTicketFactory`, …).
- Request factories for API scenarios (`EnqueueRequestFactory`, `UpsertGameConfigRequestFactory`, `LateJoinRequestFactory`).
- Rely on each project’s `GlobalUsings.cs`; do not duplicate those usings in every file.

Factories must create valid defaults, allow scenario-specific overrides, and keep the **Act** section calling production code (or the HTTP endpoint / engine tick) explicitly.

## Verify observable state and required side effects

Focus assertions on outcomes visible through the public contract of the suite:

| Suite | Observable outcomes |
|---|---|
| Infrastructure | Result success/failure, error codes, reloaded entity state from the store |
| API | HTTP status, response DTOs, subsequent GET ticket/session/config |

When verifying matching:

```csharp
await RunEngineOnceAsync();

var ticket1 = await ticketStore.GetAsync(t1.Value.Ticket.Id);
ticket1.Value.Status.Should().Be(TicketStatus.Matched);
ticket1.Value.SessionId.Should().NotBeNull();
```

Do not verify private implementation details of Redis key layouts unless the test’s purpose is explicitly that contract.

## Keep each test focused and deterministic

- One behavioral scenario per test; multiple assertions are fine when they describe one outcome.
- Make matching deterministic: enqueue with known latencies and enqueue times, then call `RunOnceAsync` / `RunMatchOnceAsync` once (or a fixed number of times).
- Do **not** rely on the Worker host’s background timer in tests.
- Prefer `[Theory]` / `MemberData` when several inputs share the same integration behavior and assertions; keep separate `[Fact]`s when error codes, HTTP statuses, or side effects differ.

## Running integration tests

Prerequisites: Docker running, current .NET SDK.

```bash
dotnet test tests/Rovio.Matchmaking.Infrastructure.Tests.Integration
dotnet test tests/Rovio.Matchmaking.Api.Tests.Integration
```

Filter example:

```bash
dotnet test tests/Rovio.Matchmaking.Infrastructure.Tests.Integration --filter FullyQualifiedName~MatchmakingEngine
```

If Docker is unavailable, tests fail at `RequireDocker()` with the fixture startup error — fix Docker (or Testcontainers) rather than commenting out tests.
