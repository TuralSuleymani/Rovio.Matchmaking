using Rovio.Matchmaking.Domain.Services;

namespace Rovio.Matchmaking.Domain.Tests.Unit.Services;

public sealed class MatchingServiceTests
{
    private readonly MatchingService _matchingService = new();
    private readonly MatchRegion _region = MatchRegionFactory.Create();

    [Fact]
    public void SelectMatchGroup_WhenCandidateCountIsBelowMinimum_ShouldReturnEmpty()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                latencyMs: DefaultLatencyMs,
                enqueuedAt: OlderEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().BeEmpty();
    }

    [Fact]
    public void SelectMatchGroup_WhenOlderTicketsAreCompatible_ShouldPreferLongerWaitingPlayers()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: NewPlayerId,
                latencyMs: DefaultLatencyMs,
                enqueuedAt: RecentEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: OlderPlayerId1,
                latencyMs: CompatibleLatencyMs,
                enqueuedAt: OlderEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: OlderPlayerId2,
                latencyMs: CompatibleLatencyMs + CompatibleLatencyOffsetMs,
                enqueuedAt: OldestEnqueueAtWithOffset)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Select(t => t.PlayerId.Value).Should().BeEquivalentTo([OlderPlayerId1, OlderPlayerId2]);
        group.Should().BeInAscendingOrder(t => t.EnqueuedAt);
    }

    [Fact]
    public void SelectMatchGroup_WhenLatencyGapIsTooLargeForFreshTickets_ShouldReturnEmpty()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo(
            latencyPolicy: LatencyPolicyFactory.Create());
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                latencyMs: DefaultLatencyMs,
                enqueuedAt: RecentEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: SecondPlayerId,
                latencyMs: IncompatibleLatencyMs,
                enqueuedAt: RecentEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().BeEmpty();
    }

    [Fact]
    public void SelectMatchGroup_WhenWaitTimeExpandsLatencyTolerance_ShouldReturnMatch()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo(
            latencyPolicy: LatencyPolicyFactory.CreateDefault());
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                latencyMs: DefaultLatencyMs,
                enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: SecondPlayerId,
                latencyMs: IncompatibleLatencyMs,
                enqueuedAt: OldestEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().HaveCount(DuoMaxPlayers);
    }

    [Fact]
    public void SelectMatchGroup_WhenMoreCompatiblePlayersThanMaximum_ShouldCapAtMaximum()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(playerId: DefaultPlayerId, enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(playerId: SecondPlayerId, enqueuedAt: OlderEnqueueAt),
            MatchTicketFactory.CreateQueued(playerId: ThirdPlayerId, enqueuedAt: RecentEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().HaveCount(DuoMaxPlayers);
        group.Select(t => t.PlayerId.Value).Should().BeEquivalentTo([DefaultPlayerId, SecondPlayerId]);
        group.Should().BeInAscendingOrder(t => t.EnqueuedAt);
    }

    [Fact]
    public void SelectMatchGroup_WhenFreshPlayerToleranceIsStricter_ShouldNotMatchOnLongWaiterToleranceAlone()
    {
        // Arrange: long waiter allows ~200ms; fresh player allows base 50ms; gap is 180ms.
        var config = GameMatchConfigFactory.CreateDuo(
            latencyPolicy: LatencyPolicyFactory.CreateDefault());
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: OlderPlayerId1,
                latencyMs: DefaultLatencyMs,
                enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: NewPlayerId,
                latencyMs: DefaultLatencyMs + 180,
                enqueuedAt: RecentEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().BeEmpty();
    }

    [Fact]
    public void SelectMatchGroup_WhenPlayersAreSeedCompatibleButNotPairwiseCompatible_ShouldNotFormGroup()
    {
        // Arrange: seed 100; A 50; B 150; each is within 50 of seed, but A↔B is 100.
        var config = GameMatchConfigFactory.Create(
            minPlayers: TrioMaxPlayers,
            maxPlayers: TrioMaxPlayers,
            latencyPolicy: LatencyPolicyFactory.Create(
                baseMaxLatencyDeltaMs: DefaultLatencyDeltaMs,
                expansionIntervalSeconds: DefaultExpansionIntervalSeconds,
                expansionStepMs: ZeroExpansionStepMs,
                absoluteMaxLatencyDeltaMs: DefaultLatencyDeltaMs));
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                latencyMs: 100,
                enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: SecondPlayerId,
                latencyMs: 50,
                enqueuedAt: OlderEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: ThirdPlayerId,
                latencyMs: 150,
                enqueuedAt: RecentEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().BeEmpty();
    }

    [Fact]
    public void SelectMatchGroup_WhenCandidatesIncludeNonQueuedTickets_ShouldIgnoreThem()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var matched = MatchTicketFactory.RehydrateMatched(Id<GameSession>.New());
        var candidates = new[]
        {
            matched,
            MatchTicketFactory.CreateQueued(playerId: DefaultPlayerId, enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(playerId: SecondPlayerId, enqueuedAt: OlderEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Select(t => t.PlayerId.Value).Should().BeEquivalentTo([DefaultPlayerId, SecondPlayerId]);
        group.Should().NotContain(matched);
        group.Should().BeInAscendingOrder(t => t.EnqueuedAt);
    }

    [Fact]
    public void SelectMatchGroup_WhenFirstSeedCannotFormGroup_ShouldTryLaterSeedAndReturnOldestFirst()
    {
        // Arrange: oldest ticket is incompatible with both; the two newer tickets match each other.
        var config = GameMatchConfigFactory.CreateDuo(
            latencyPolicy: LatencyPolicyFactory.Create(
                baseMaxLatencyDeltaMs: DefaultLatencyDeltaMs,
                expansionStepMs: ZeroExpansionStepMs,
                absoluteMaxLatencyDeltaMs: DefaultLatencyDeltaMs));
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: OlderPlayerId1,
                latencyMs: DefaultLatencyMs,
                enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: OlderPlayerId2,
                latencyMs: IncompatibleLatencyMs,
                enqueuedAt: OlderEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: NewPlayerId,
                latencyMs: IncompatibleLatencyMs + CompatibleLatencyOffsetMs,
                enqueuedAt: RecentEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Select(t => t.PlayerId.Value).Should().Equal(OlderPlayerId2, NewPlayerId);
        group.Should().BeInAscendingOrder(t => t.EnqueuedAt);
    }

    [Fact]
    public void SelectMatchGroup_WhenCompatibleCountIsBetweenMinAndMax_ShouldReturnAllCompatible()
    {
        // Arrange
        var config = GameMatchConfigFactory.Create(
            minPlayers: DefaultMinPlayers,
            maxPlayers: DefaultMaxPlayers);
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(playerId: DefaultPlayerId, enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(playerId: SecondPlayerId, enqueuedAt: OlderEnqueueAt),
            MatchTicketFactory.CreateQueued(playerId: ThirdPlayerId, enqueuedAt: RecentEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().HaveCount(MidCapacityPlayerCount);
        group.Select(t => t.PlayerId.Value)
            .Should()
            .Equal(DefaultPlayerId, SecondPlayerId, ThirdPlayerId);
    }

    [Fact]
    public void SelectMatchGroup_WhenLatencyDeltaEqualsAllowedMinimum_ShouldMatch()
    {
        // Arrange: fresh tickets, base tolerance 50, gap exactly 50.
        var config = GameMatchConfigFactory.CreateDuo(
            latencyPolicy: LatencyPolicyFactory.Create(
                baseMaxLatencyDeltaMs: DefaultLatencyDeltaMs,
                expansionStepMs: ZeroExpansionStepMs,
                absoluteMaxLatencyDeltaMs: DefaultLatencyDeltaMs));
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                latencyMs: DefaultLatencyMs,
                enqueuedAt: RecentEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: SecondPlayerId,
                latencyMs: DefaultLatencyMs + DefaultLatencyDeltaMs,
                enqueuedAt: RecentEnqueueAt)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().HaveCount(DuoMaxPlayers);
    }

    [Fact]
    public void SelectMatchGroup_WhenEnqueueTimesAreEqual_ShouldBreakTiesByTicketId()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var firstId = Id<MatchTicket>.Create(Guid.Parse("00000000-0000-0000-0000-000000000001")).Value;
        var secondId = Id<MatchTicket>.Create(Guid.Parse("00000000-0000-0000-0000-000000000002")).Value;
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: SecondPlayerId,
                enqueuedAt: OlderEnqueueAt,
                id: secondId),
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                enqueuedAt: OlderEnqueueAt,
                id: firstId)
        };

        // Act
        var group = SelectGroup(candidates, config);

        // Assert
        group.Should().HaveCount(DuoMaxPlayers);
        group[0].Id.Should().Be(firstId);
        group[1].Id.Should().Be(secondId);
    }

    [Fact]
    public void SelectMatchGroup_WhenCandidatesAreEmpty_ShouldReturnEmpty()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();

        // Act
        var group = SelectGroup([], config);

        // Assert
        group.Should().BeEmpty();
    }

    [Fact]
    public void SelectMatchGroup_WhenCandidatesAreNull_ShouldFail()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();

        // Act
        var result = _matchingService.SelectMatchGroup(null!, config, _region, DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchingInput);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void SelectMatchGroup_WhenConfigIsNull_ShouldFail()
    {
        // Arrange
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(playerId: DefaultPlayerId),
            MatchTicketFactory.CreateQueued(playerId: SecondPlayerId)
        };

        // Act
        var result = _matchingService.SelectMatchGroup(candidates, null!, _region, DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchingInput);
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void SelectMatchGroup_WhenRegionIsNull_ShouldFail()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(playerId: DefaultPlayerId),
            MatchTicketFactory.CreateQueued(playerId: SecondPlayerId)
        };

        // Act
        var result = _matchingService.SelectMatchGroup(candidates, config, null!, DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMatchingInput);
    }

    [Fact]
    public void SelectMatchGroup_WhenCandidatesSpanMultipleRegions_ShouldFail()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                region: DefaultRegion,
                enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: SecondPlayerId,
                region: NaRegion,
                enqueuedAt: OlderEnqueueAt)
        };

        // Act
        var result = _matchingService.SelectMatchGroup(candidates, config, _region, DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.MismatchedMatchShard);
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void SelectMatchGroup_WhenDuplicatePlayersAreQueued_ShouldFail()
    {
        // Arrange
        var config = GameMatchConfigFactory.CreateDuo();
        var candidates = new[]
        {
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                enqueuedAt: OldestEnqueueAt),
            MatchTicketFactory.CreateQueued(
                playerId: DefaultPlayerId,
                enqueuedAt: OlderEnqueueAt)
        };

        // Act
        var result = _matchingService.SelectMatchGroup(candidates, config, _region, DefaultNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.DuplicateQueuedPlayer);
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
    }

    private IReadOnlyList<MatchTicket> SelectGroup(
        IReadOnlyList<MatchTicket> candidates,
        GameMatchConfig config)
    {
        var result = _matchingService.SelectMatchGroup(candidates, config, _region, DefaultNow);
        result.IsSuccess.Should().BeTrue(because: result.IsFailure ? result.Error.ErrorMessage : null);
        return result.Value;
    }
}
