
namespace Rovio.Matchmaking.Api.Tests.Integration;

public sealed class GamesApiTests(ApiFixture fixture) : BaseApiSpec(fixture)
{
    [Fact]
    public async Task GetConfig_WhenSeeded_ShouldReturnAngryBirds2Defaults()
    {
        RequireDocker();

        var response = await Client.GetAsync($"/api/v1/games/{AngryBirds2GameId}/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<GameMatchConfigDto>(JsonOptions);
        config.Should().NotBeNull();
        config!.GameId.Should().Be(AngryBirds2GameId);
        config.MinPlayers.Should().Be(DefaultMinPlayers);
        config.MaxPlayers.Should().Be(DefaultMaxPlayers);
    }

    [Fact]
    public async Task PutConfig_WhenValid_ShouldUpdatePostgresAndProjection()
    {
        RequireDocker();
        var gameId = UniqueGameId("cfg");
        var body = UpsertGameConfigRequestFactory.Create(
            minPlayers: DefaultMinPlayers,
            maxPlayers: TrioMaxPlayers,
            maxQueueDepth: ValidMaxQueueDepth);

        await PutConfigAsync(gameId, body);

        var config = await Client.GetFromJsonAsync<GameMatchConfigDto>(
            $"/api/v1/games/{gameId}/config", JsonOptions);

        config.Should().NotBeNull();
        config!.MaxPlayers.Should().Be(TrioMaxPlayers);
        config.MaxQueueDepth.Should().Be(ValidMaxQueueDepth);
    }
}
