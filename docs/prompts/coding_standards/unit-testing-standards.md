This document reflects my practical experience with unit testing and the approach I follow in my day-to-day development work. I adopted these standards and practices several years ago and have continued to refine them over time.

They are not intended to represent a perfect or universally applicable set of rules. Instead, they provide a consistent and practical foundation for writing readable, maintainable, and reliable unit tests. These guidelines may continue to evolve as new lessons are learned, project requirements change, and better approaches emerge.
**Only examples were adapted to the current project.**

#  Testing Standards

These standards apply to unit tests across the domain and application layers. Tests must be readable, deterministic, focused on observable behavior, and independent of external infrastructure.

##  Test naming convention

Use the following format:

```text
MethodName_WhenCondition_ShouldExpectedResult
```

The name must describe:

1. the method or behavior under test;
2. the relevant condition or scenario;
3. the expected observable result.

Examples:

```csharp
Create_WhenValidArgumentProvided_ShouldCreateFormedSession()

Create_WhenPlayerCountExceedsMaximum_ShouldFail()

EnsureCanLateJoin_WhenLateJoinDisabled_ShouldFail()

TryAddPlayer_WhenFillingLastSlot_ShouldBecomeFull()

LateJoinAsync_WhenTicketAlreadyMatchedToSession_ShouldReturnRefreshedSession()
```

Use this convention consistently for synchronous and asynchronous tests. Include the `Async` suffix when it is part of the production method name.

## Use Arrange / Act / Assert

Tests must clearly separate setup, execution, and verification:

- **Arrange** creates the system state, test data, and dependency behavior.
- **Act** invokes the behavior under test.
- **Assert** verifies the observable outcome.

```csharp
[Fact]
public void TryAddPlayer_WhenFillingLastSlot_ShouldBecomeFull()
{
    // Arrange
    var session = GameSessionFactory.CreateWithOpenSlot();
    var joiner = PlayerIdFactory.CreateThird();

    // Act
    var result = session.TryAddPlayer(joiner);

    // Assert
    result.IsSuccess.Should().BeTrue();
    session.PlayerIds.Should().Contain(joiner);
    session.Status.Should().Be(SessionStatus.Full);
    session.CanLateJoin.Should().BeFalse();
}
```

Do not add an empty `Arrange` section when no setup is required:

```csharp
[Fact]
public void Create_WhenRegionIsNull_ShouldFail()
{
    // Act
    var result = GameSession.Create(
        GameIdFactory.Create(),
        null!,
        PlayerCapacityFactory.Create(),
        allowLateJoin: true,
        [PlayerIdFactory.Create(), PlayerIdFactory.CreateSecond()],
        DefaultNow);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(ErrorCodes.InvalidSession);
}
```

For non-trivial tests, keep the `Arrange`, `Act`, and `Assert` comments because they make the test flow immediately visible.

##  Use Theory for multiple equivalent input cases

Use `[Theory]` when multiple inputs exercise the same behavior and require the same assertions. Do not create separate `[Fact]` tests when only the supplied input changes.

For complex domain inputs, prefer `MemberData` over unreadable `InlineData` declarations.

For example, repeated invalid-argument scenarios for `GameSession.Create` can be represented as one theory:

```csharp
public static TheoryData<MatchRegion, PlayerCapacity, IReadOnlyCollection<PlayerId>>
    InvalidSessionArguments =>
    new()
    {
        {
            null!,
            PlayerCapacityFactory.Create(),
            [PlayerIdFactory.Create(), PlayerIdFactory.CreateSecond()]
        },
        {
            MatchRegionFactory.Create(),
            null!,
            [PlayerIdFactory.Create(), PlayerIdFactory.CreateSecond()]
        },
        {
            MatchRegionFactory.Create(),
            PlayerCapacityFactory.Create(),
            null!
        }
    };

[Theory]
[MemberData(nameof(InvalidSessionArguments))]
public void Create_WhenRequiredArgumentIsNull_ShouldFail(
    MatchRegion region,
    PlayerCapacity playerCapacity,
    IReadOnlyCollection<PlayerId> playerIds)
{
    // Act
    var result = GameSession.Create(
        GameIdFactory.Create(),
        region,
        playerCapacity,
        allowLateJoin: true,
        playerIds,
        DefaultNow);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(ErrorCodes.InvalidSession);
}
```

Keep separate `[Fact]` tests when scenarios have different business meaning, setup, expected error codes, side effects, or diagnostic value.

## Do not use magic values

Test data must communicate its purpose. Prefer named constants, shared test defaults, value-object factories, and explicitly named local variables over unexplained literals.

Avoid embedding an unexplained literal in otherwise meaningful test data:

```csharp
var ticketDto = new TicketDto(
    ticket.Id.ToString(),
    ThirdPlayerId,
    AngryBirds2GameId,
    DefaultRegion,
    50,
    ticket.EnqueuedAt,
    TicketStatus.Queued.Name,
    SessionId: null);
```

Prefer a named test default:

```csharp
var ticketDto = new TicketDto(
    ticket.Id.ToString(),
    ThirdPlayerId,
    AngryBirds2GameId,
    DefaultRegion,
    DefaultLatencyMs,
    ticket.EnqueuedAt,
    TicketStatus.Queued.Name,
    SessionId: null);
```

Use meaningful shared defaults when the same values are reused across the test suite:

```csharp
var ticketDto = new TicketDto(
    ticket.Id.ToString(),
    ThirdPlayerId,
    AngryBirds2GameId,
    DefaultRegion,
    DefaultLatencyMs,
    ticket.EnqueuedAt,
    TicketStatus.Queued.Name,
    SessionId: null);
```

Assertions should also avoid unexplained values:

```csharp
result.Value.OpenSlots.Should().Be(DefaultMaxPlayers - playerIds.Length);
result.Value.StartedAt.Should().Be(DefaultNow);
```

A literal is acceptable when its meaning is obvious from the API or assertion, such as `Received(1)` or `OpenSlots.Should().Be(0)`.

## Prefer Result assertions for expected failures

When a method returns `Result`, `UnitResult`, or another explicit outcome type, verify expected business failures through that result.

```csharp
[Fact]
public void EnsureCanLateJoin_WhenLateJoinDisabled_ShouldFail()
{
    // Arrange
    var session = GameSessionFactory.CreateWithOpenSlot(allowLateJoin: false);

    // Act
    var result = session.EnsureCanLateJoin();

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(ErrorCodes.LateJoinDisabled);
    result.Error.ErrorType.Should().Be(ErrorType.Validation);
}
```

Application-service failures must be tested in the same way:

```csharp
[Fact]
public async Task LateJoinAsync_WhenRegionDoesNotMatch_ShouldFail()
{
    // Arrange
    var session = GameSessionFactory.CreateWithOpenSlot();
    var request = LateJoinRequestFactory.Create(region: NaRegion);

    _sessionStore.GetAsync(session.Id, Arg.Any<CancellationToken>())
        .Returns(Result.Success<GameSession, IDomainError>(session));

    _configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
        .Returns(GameMatchConfigFactory.Create());

    // Act
    var result = await _sut.LateJoinAsync(session.Id, request);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(ErrorCodes.RegionMismatch);
    result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
}
```

Do not use exception assertions for expected validation, conflict, not-found, capacity, or other business-rule failures.

Exception assertions are appropriate only when an exception is the intended contract for a programming error or an unexpected infrastructure failure. When the application translates an infrastructure exception into a `Result`, assert the translated result instead:

```csharp
_configRuntime.GetAsync(GameIdFactory.Create(), Arg.Any<CancellationToken>())
    .ThrowsAsync(new InvalidOperationException());

var result = await _sut.LateJoinAsync(session.Id, request);

result.IsFailure.Should().BeTrue();
result.Error.Code.Should().Be(ErrorCodes.RedisUnavailable);
```

## Use test factories and builders

Use factories or builders to create valid domain objects and requests with sensible defaults.

```csharp
var session = GameSessionFactory.CreateWithOpenSlot();
var ticket = MatchTicketFactory.CreateQueued(playerId: ThirdPlayerId);
var request = LateJoinRequestFactory.Create();
```

Factories and builders must:

- create valid objects by default;
- allow scenario-specific overrides;
- centralize repeated setup;
- use deterministic values where assertions depend on identity or time;
- keep individual tests focused on the behavior under test;
- avoid hiding details that are essential to understanding the scenario.

Examples of scenario-specific overrides:

```csharp
var session = GameSessionFactory.CreateWithOpenSlot(allowLateJoin: false);
```

```csharp
var session = GameSessionFactory.Rehydrate(
    SessionStatus.Full,
    allowLateJoin: true,
    playerCapacity: PlayerCapacityFactory.CreateDuo(),
    playerIds:
    [
        PlayerIdFactory.Create(),
        PlayerIdFactory.CreateSecond()
    ]);
```

```csharp
var request = LateJoinRequestFactory.Create(region: NaRegion);
```

Do not move the behavior being tested into a factory. The test must invoke the production method explicitly in the `Act` section unless the factory itself is the subject under test.

## Name the system under test consistently

For service tests, store the class under test in a field named `_sut`. Dependencies should be represented by interfaces and substituted independently.

```csharp
private readonly ISessionStore _sessionStore = Substitute.For<ISessionStore>();
private readonly ITicketStore _ticketStore = Substitute.For<ITicketStore>();
private readonly IGameConfigRuntime _configRuntime = Substitute.For<IGameConfigRuntime>();
private readonly IQueueService _queueService = Substitute.For<IQueueService>();
private readonly SessionService _sut;

public SessionServiceTests()
{
    _sut = new SessionService(
        _sessionStore,
        _ticketStore,
        _configRuntime,
        _queueService);
}
```

The test should interact with `_sut`, not instantiate a second service instance inside individual test methods unless construction itself varies by scenario.

## Verify observable state, returned values, and required interactions

Assertions must focus on outcomes visible through the public contract:

- returned success or failure;
- error code and error type;
- resulting entity state;
- returned DTO values;
- emitted domain events;
- required calls to dependencies;
- absence of calls when a dependency must not be invoked.

State and return-value assertions:

```csharp
result.IsSuccess.Should().BeTrue();
result.Value.Status.Should().Be(SessionStatus.Formed);
result.Value.OpenSlots.Should().Be(DefaultMaxPlayers - playerIds.Length);
```

Required interaction assertion:

```csharp
await _sessionStore.Received(1).LateJoinAsync(
    session.Id,
    GameIdFactory.Create(),
    MatchRegionFactory.Create(),
    ticket.Id,
    PlayerIdFactory.CreateThird(),
    Arg.Any<CancellationToken>());
```

Negative interaction assertion:

```csharp
await _sessionStore.DidNotReceive().LateJoinAsync(
    Arg.Any<Id<GameSession>>(),
    Arg.Any<GameId>(),
    Arg.Any<MatchRegion>(),
    Arg.Any<Id<MatchTicket>>(),
    Arg.Any<PlayerId>(),
    Arg.Any<CancellationToken>());
```

Do not verify implementation details that are not part of the required behavior. Interaction assertions should be used when the interaction itself is significant, such as persistence, publishing, idempotency, or ensuring that an invalid operation stops before a write occurs.

## Keep each test focused on one behavioral scenario

A test may contain multiple assertions when they collectively describe one outcome.

For example, when a player fills the final slot, it is appropriate to verify the returned success, player membership, session status, and late-join availability in the same test:

```csharp
result.IsSuccess.Should().BeTrue();
session.PlayerIds.Should().Contain(joiner);
session.Status.Should().Be(SessionStatus.Full);
session.CanLateJoin.Should().BeFalse();
```

Do not combine unrelated conditions or multiple independent actions into one test. Create separate tests when failures would represent different business rules or require different diagnoses.
requires real infrastructure belongs in an integration or end-to-end test project, not in the unit-test suite.
