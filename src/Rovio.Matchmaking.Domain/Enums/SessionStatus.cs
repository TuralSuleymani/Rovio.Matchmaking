using Ardalis.SmartEnum;

namespace Rovio.Matchmaking.Domain.Enums;

public sealed class SessionStatus : SmartEnum<SessionStatus>
{
    public static readonly SessionStatus Formed = new(nameof(Formed), 0);
    public static readonly SessionStatus Full = new(nameof(Full), 1);

    private SessionStatus(string name, int value) : base(name, value)
    {
    }
}
