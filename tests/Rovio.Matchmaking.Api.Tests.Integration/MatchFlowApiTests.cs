
namespace Rovio.Matchmaking.Api.Tests.Integration;

public sealed class MatchFlowApiTests(ApiFixture fixture) : BaseApiSpec(fixture)
{
    [Fact]
    public async Task Enqueue_WhenCompatiblePlayers_ShouldAssignSharedSession()
    {
        RequireDocker();
        var gameId = UniqueGameId("match");
        var region = UniqueRegion();
        await EnsureGameConfigAsync(
            gameId,
            minPlayers: DefaultMinPlayers,
            maxPlayers: DuoMaxPlayers);

        var playerA = UniquePlayerId("a");
        var playerB = UniquePlayerId("b");
        var ticketA = await EnqueueSuccessAsync(gameId, playerA, region, DefaultLatencyMs);
        var ticketB = await EnqueueSuccessAsync(gameId, playerB, region, CompatibleLatencyMs);

        await RunMatchOnceAsync();

        var matchedA = await GetTicketAsync(gameId, ticketA.TicketId);
        var matchedB = await GetTicketAsync(gameId, ticketB.TicketId);
        matchedA.Status.Should().Be(TicketStatus.Matched.Name);
        matchedB.Status.Should().Be(TicketStatus.Matched.Name);
        matchedA.SessionId.Should().NotBeNullOrEmpty();
        matchedA.SessionId.Should().Be(matchedB.SessionId);

        var session = await Client.GetFromJsonAsync<SessionDto>(
            $"/api/v1/sessions/{matchedA.SessionId}", JsonOptions);
        session.Should().NotBeNull();
        session!.PlayerIds.Should().Contain([playerA, playerB]);
    }

    [Fact]
    public async Task LateJoin_WhenOpenSlot_ShouldAddPlayerToSession()
    {
        RequireDocker();
        var gameId = UniqueGameId("late");
        var region = UniqueRegion();
        await EnsureGameConfigAsync(
            gameId,
            minPlayers: DefaultMinPlayers,
            maxPlayers: TrioMaxPlayers,
            allowLateJoin: true);

        var playerA = UniquePlayerId("a");
        var playerB = UniquePlayerId("b");
        var playerC = UniquePlayerId("c");
        var ticketA = await EnqueueSuccessAsync(gameId, playerA, region, DefaultLatencyMs);
        await EnqueueSuccessAsync(gameId, playerB, region, CompatibleLatencyMs);

        await RunMatchOnceAsync();

        var matchedA = await GetTicketAsync(gameId, ticketA.TicketId);
        matchedA.Status.Should().Be(TicketStatus.Matched.Name);
        matchedA.SessionId.Should().NotBeNullOrEmpty();

        var join = await Client.PostAsJsonAsync(
            $"/api/v1/sessions/{matchedA.SessionId}/join",
            LateJoinRequestFactory.Create(playerC, region, CompatibleLatencyOffsetMs));

        join.EnsureSuccessStatusCode();
        var session = await join.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);
        session.Should().NotBeNull();
        session!.PlayerIds.Should().Contain(playerC);
        session.PlayerIds.Should().HaveCount(TrioMaxPlayers);
    }
}
