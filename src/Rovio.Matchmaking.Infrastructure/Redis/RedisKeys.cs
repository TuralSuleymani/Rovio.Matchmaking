namespace Rovio.Matchmaking.Infrastructure.Redis;

public static class RedisKeys
{
    public static string Config(string gameId) => $"mm:config:{gameId}";
    public const string ConfigIndex = "mm:config:index";
    public static string Ticket(string ticketId) => $"mm:ticket:{ticketId}";
    public static string Player(string gameId, string playerId) => $"mm:player:{gameId}:{playerId}";
    public static string Queue(string gameId, string region) => $"mm:queue:{gameId}:{region}";
    public static string Session(string sessionId) => $"mm:session:{sessionId}";
    public static string OpenSessions(string gameId, string region) => $"mm:open:{gameId}:{region}";
    public static string Lock(string gameId, string region) => $"mm:lock:{gameId}:{region}";
}
