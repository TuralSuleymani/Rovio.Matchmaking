Write unit tests for @<TargetTypeOrFile>.
Follow @docs/coding_standards/unit-testing-standards.md exactly. Also mirror existing tests in the same project/folder for tone, helpers, and factories.
## Which project
Choose the correct unit-test project (never put these in Integration):
- Shared kernel → `tests/Rovio.Domain.Common.Tests.Unit`
- Domain entities / VOs / MatchingService → `tests/Rovio.Matchmaking.Domain.Tests.Unit`
- Application services → `tests/Rovio.Matchmaking.Application.Tests.Unit`
## Hard rules from the standards
- Naming: `MethodName_WhenCondition_ShouldExpectedResult` (keep `Async` when it is on the production method)
- AAA: `// Arrange`, `// Act`, `// Assert` for non-trivial tests; omit empty Arrange when there is no setup
- Use `[Theory]` + `MemberData` (or `InlineData`) when only the input changes and assertions are identical; keep separate `[Fact]`s when error codes, side effects, or business meaning differ
- No magic values: `MatchmakingTestData`, `*Factory`, named locals; literals only when obvious (`Received(1)`, `.Be(0)`)
- Expected business failures → assert `Result` / `UnitResult` (`IsFailure`, `Error.Code` via `ErrorCodes.*`, `ErrorType`) — never `Assert.Throws` for validation/conflict/not-found
- Exception stubs only when infrastructure throws and the SUT translates to a Result — then assert the translated Result
- Factories create valid defaults + scenario overrides; the test must invoke the production method in Act (don’t hide the behavior in a factory)
- Application services: field `_sut`; dependencies as `Substitute.For<I...>()`; construct `_sut` in the constructor
- Assert observable outcomes only: return values, entity state, DTO fields, required `Received` / `DidNotReceive` when the interaction itself matters
- One behavioral scenario per test (multiple assertions OK if they describe one outcome)
- Pure unit tests: no Docker, Redis, Postgres, HTTP, Testcontainers
- Use GlobalUsings — do not add duplicate usings already covered by GlobalUsings.cs
## Process
1. Read the target production code and existing sibling tests
2. List missing edge cases briefly (happy path, validation, conflict/invariants, boundaries, null inputs where Result is used)
3. Add only the missing tests; extend factories / MatchmakingTestData only if a shared value is missing
4. Run the relevant project with a filter, then fix until green:
   - `dotnet test tests/Rovio.Matchmaking.Domain.Tests.Unit --filter FullyQualifiedName~<Class>`
   - `dotnet test tests/Rovio.Matchmaking.Application.Tests.Unit --filter FullyQualifiedName~<Class>`
   - `dotnet test tests/Rovio.Domain.Common.Tests.Unit --filter FullyQualifiedName~<Class>`
Target: @<file or type>
Focus: <optional — e.g. TryAddPlayer edge cases only / LateJoinAsync failure paths>