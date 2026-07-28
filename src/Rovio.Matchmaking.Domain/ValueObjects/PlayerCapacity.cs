namespace Rovio.Matchmaking.Domain.ValueObjects;

public sealed record PlayerCapacity
{
    private PlayerCapacity(int minPlayerCount, int maxPlayerCount)
    {
        MinPlayerCount = minPlayerCount;
        MaxPlayerCount = maxPlayerCount;
    }

    public int MinPlayerCount { get; }
    public int MaxPlayerCount { get; }

    public static Result<PlayerCapacity, DomainError> Create(int minPlayerCount, int maxPlayerCount)
    {
        if (minPlayerCount < 2)
        {
            return DomainError.Validation(
                "Minimum player count must be at least 2.",
                code: DomainErrorCodes.InvalidMatchSize);
        }

        if (maxPlayerCount < minPlayerCount)
        {
            return DomainError.Validation(
                "Maximum player count must be greater than or equal to minimum player count.",
                code: DomainErrorCodes.InvalidMatchSize);
        }

        return new PlayerCapacity(minPlayerCount, maxPlayerCount);
    }

    public bool CanStart(int playerCount) => playerCount >= MinPlayerCount;

    public bool IsFull(int playerCount) => playerCount >= MaxPlayerCount;

    public bool CanAccept(int playerCount) => playerCount < MaxPlayerCount;

    public bool Contains(int playerCount) =>
        playerCount >= MinPlayerCount && playerCount <= MaxPlayerCount;
}
