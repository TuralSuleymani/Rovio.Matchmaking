using System.Text.Json;
using Rovio.Matchmaking.Application.Models;

namespace Rovio.Matchmaking.Api.Tests.Integration.Fixtures;

[Collection(ApiCollection.Name)]
public abstract class BaseApiSpec
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected BaseApiSpec(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    protected ApiFixture Fixture { get; }

    protected HttpClient Client => Fixture.Client;

    protected void RequireDocker()
    {
        if (!Fixture.DockerAvailable)
        {
            throw new InvalidOperationException(
                $"Docker/Testcontainers unavailable: {Fixture.StartupError}");
        }
    }

    protected static string UniqueGameId(string prefix = "g") =>
        $"{prefix}-{Guid.NewGuid():N}";

    protected static string UniquePlayerId(string prefix = "p") =>
        $"{prefix}-{Guid.NewGuid():N}";

    protected static string UniqueRegion(string prefix = "r") =>
        $"{prefix}{Guid.NewGuid():N}"[..8];

    protected async Task PutConfigAsync(string gameId, UpsertGameConfigRequest request)
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/games/{gameId}/config", request);
        response.EnsureSuccessStatusCode();
    }

    protected async Task EnsureGameConfigAsync(
        string gameId,
        int? minPlayers = null,
        int? maxPlayers = null,
        bool allowLateJoin = true,
        bool enabled = true,
        int? maxQueueDepth = null)
    {
        await PutConfigAsync(
            gameId,
            UpsertGameConfigRequestFactory.Create(
                minPlayers: minPlayers,
                maxPlayers: maxPlayers,
                allowLateJoin: allowLateJoin,
                enabled: enabled,
                maxQueueDepth: maxQueueDepth));
    }

    protected async Task<(HttpResponseMessage Response, TicketDto? Ticket)> EnqueueAsync(
        string gameId,
        string? playerId = null,
        string? region = null,
        int? latencyMs = null)
    {
        var request = EnqueueRequestFactory.Create(playerId, region, latencyMs);
        var response = await Client.PostAsJsonAsync($"/api/v1/games/{gameId}/queue", request);
        TicketDto? ticket = null;
        if (response.IsSuccessStatusCode)
        {
            ticket = await response.Content.ReadFromJsonAsync<TicketDto>(JsonOptions);
        }

        return (response, ticket);
    }

    protected async Task<TicketDto> EnqueueSuccessAsync(
        string gameId,
        string? playerId = null,
        string? region = null,
        int? latencyMs = null)
    {
        var (response, ticket) = await EnqueueAsync(gameId, playerId, region, latencyMs);
        response.EnsureSuccessStatusCode();
        return ticket!;
    }

    protected async Task<TicketDto> GetTicketAsync(string gameId, string ticketId)
    {
        return (await Client.GetFromJsonAsync<TicketDto>(
            $"/api/v1/games/{gameId}/queue/{ticketId}", JsonOptions))!;
    }

    protected Task RunMatchOnceAsync() => Fixture.RunMatchOnceAsync();
}
