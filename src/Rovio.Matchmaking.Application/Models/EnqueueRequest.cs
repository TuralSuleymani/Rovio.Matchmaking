namespace Rovio.Matchmaking.Application.Models;

public sealed record EnqueueRequest(string PlayerId, string Region, int LatencyMs);
