
namespace Rovio.Matchmaking.Tests.Data.Fakes;

public sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset? utcNow = null)
    {
        UtcNow = utcNow ?? DefaultNow;
    }

    public DateTimeOffset UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
