using Ardalis.SmartEnum;

namespace Rovio.Matchmaking.Domain.Enums;

public sealed class TicketStatus : SmartEnum<TicketStatus>
{
    public static readonly TicketStatus Queued = new(nameof(Queued), 0);
    public static readonly TicketStatus Matched = new(nameof(Matched), 1);
    public static readonly TicketStatus Cancelled = new(nameof(Cancelled), 2);

    private TicketStatus(string name, int value) : base(name, value)
    {
    }
}
