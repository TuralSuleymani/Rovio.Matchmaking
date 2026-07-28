
namespace Rovio.Matchmaking.Infrastructure.Tests.Integration.Fixtures;

[Collection(InfrastructureCollection.Name)]
public abstract class BaseInfrastructureSpec
{
    protected BaseInfrastructureSpec(InfrastructureFixture fixture)
    {
        Fixture = fixture;
    }

    protected InfrastructureFixture Fixture { get; }

    protected void RequireDocker()
    {
        if (!Fixture.DockerAvailable)
        {
            throw new InvalidOperationException(
                $"Docker/Testcontainers unavailable: {Fixture.StartupError}");
        }
    }

    protected T Resolve<T>() where T : notnull => Fixture.Resolve<T>();

    protected static GameId UniqueGameId(string prefix = "g") =>
        GameId.Create($"{prefix}-{Guid.NewGuid():N}").Value;

    protected static PlayerId UniquePlayerId(string prefix = "p") =>
        PlayerId.Create($"{prefix}-{Guid.NewGuid():N}").Value;

    protected static MatchRegion UniqueRegion(string prefix = "r") =>
        MatchRegion.Create($"{prefix}{Guid.NewGuid():N}"[..8]).Value;
}
