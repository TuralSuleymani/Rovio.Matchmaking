namespace Rovio.Matchmaking.Application.Models;

public sealed record LateJoinRequest(string PlayerId, string Region, int LatencyMs);
