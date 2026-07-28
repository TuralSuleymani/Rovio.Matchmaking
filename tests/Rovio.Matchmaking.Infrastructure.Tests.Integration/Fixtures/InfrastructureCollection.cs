namespace Rovio.Matchmaking.Infrastructure.Tests.Integration.Fixtures;

[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<InfrastructureFixture>
{
    public const string Name = "Infrastructure";
}
