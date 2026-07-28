namespace Rovio.Matchmaking.Application.Abstractions;

public interface IMatchmakingEngine
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}
