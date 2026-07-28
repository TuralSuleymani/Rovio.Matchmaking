using Microsoft.Extensions.Options;
using Rovio.Matchmaking.Application.Options;

namespace Rovio.Matchmaking.Tests.Data.Factories;

public static class MatchmakingOptionsFactory
{
    public static MatchmakingOptions Create(int? maxQueueDepth = null)
    {
        var options = new MatchmakingOptions();
        if (maxQueueDepth is not null)
        {
            options.MaxQueueDepth = maxQueueDepth.Value;
        }

        return options;
    }

    public static IOptions<MatchmakingOptions> CreateOptions(int? maxQueueDepth = null) =>
        Options.Create(Create(maxQueueDepth));
}
