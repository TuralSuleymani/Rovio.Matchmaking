namespace Rovio.Matchmaking.Api.Tests.Integration.Fixtures;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
