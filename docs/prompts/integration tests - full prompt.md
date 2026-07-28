Here's a Cursor-oriented prompt you can paste and reuse. Point @ at the standards doc and the production surface under test.

Full prompt (recommended)

Write integration tests for @<TargetSurface>.
Follow @docs/coding-standards/integration-testing-standards.md exactly. Also mirror existing tests in the same suite for tone, helpers, and fixture usage.
## Which suite
Choose the correct project (do not put this in a unit-test project):
- Ports / Redis / Postgres / engine → `tests/Rovio.Matchmaking.Infrastructure.Tests.Integration`
- HTTP endpoints / full host → `tests/Rovio.Matchmaking.Api.Tests.Integration`
## Hard rules from the standards
- Naming: `MethodName_WhenCondition_ShouldExpectedResult` (API may use endpoint behavior names like `Enqueue_When...`)
- AAA with `// Arrange`, `// Act`, `// Assert` for non-trivial flows
- Call `RequireDocker()` first in every test
- Inherit `BaseInfrastructureSpec` / `BaseApiSpec`; use the shared collection fixture — never start containers per test
- Real Postgres + Redis via existing Testcontainers fixture; production DI (`AddMatchmakingInfrastructure` or `WebApplicationFactory<Program>`)
- Allowed controls only: unique IDs, in-memory config already on the fixture, `FixedTimeProvider` (Infrastructure), explicit `RunOnceAsync` / `RunMatchOnceAsync` for matching
- Do NOT use NSubstitute / mock stores / stub the engine
- Isolation: `UniqueGameId` / `UniquePlayerId` / `UniqueRegion` — do not flush Redis every test; truncate Postgres only if empty/list semantics require it
- No magic values: use `MatchmakingTestData` + `*Factory` + GlobalUsings (no duplicate usings)
- Assert observable outcomes: Infrastructure → `Result` + reloaded state; API → `HttpStatusCode` + DTOs / follow-up GETs
- Deterministic matching: known latency/enqueue times, then one controlled engine tick — never wait on Worker timers
- One behavioral scenario per test
## Process
1. Read the target production code and existing sibling tests in that suite
2. List missing integration scenarios briefly (happy path, failure paths, isolation-sensitive cases)
3. Add only the missing tests (extend factories/MatchmakingTestData only if a shared value is missing)
4. Use suite helpers (`Resolve`/`CreateScope`, or `EnsureGameConfigAsync`/`EnqueueAsync`/`GetTicketAsync`/`RunMatchOnceAsync`)
5. Run the relevant project:
   - `dotnet test tests/Rovio.Matchmaking.Infrastructure.Tests.Integration --filter FullyQualifiedName~<Class>`
   - or `dotnet test tests/Rovio.Matchmaking.Api.Tests.Integration --filter FullyQualifiedName~<Class>`
6. Fix failures until green
Target: @<file or type>
Focus: <optional — e.g. late join HTTP flow only / RedisTicketStore cancel idempotency>