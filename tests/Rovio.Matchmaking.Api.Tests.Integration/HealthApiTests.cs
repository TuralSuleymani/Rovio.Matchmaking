
namespace Rovio.Matchmaking.Api.Tests.Integration;

public sealed class HealthApiTests(ApiFixture fixture) : BaseApiSpec(fixture)
{
    [Fact]
    public async Task Ready_WhenDependenciesUp_ShouldReturnOk()
    {
        RequireDocker();

        var ready = await Client.GetAsync("/health/ready");

        ready.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Live_WhenProcessUp_ShouldReturnOk()
    {
        RequireDocker();

        var live = await Client.GetAsync("/health/live");

        live.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
