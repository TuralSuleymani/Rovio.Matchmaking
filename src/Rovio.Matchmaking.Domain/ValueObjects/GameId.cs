namespace Rovio.Matchmaking.Domain.ValueObjects;

public sealed record GameId
{
    public const int MaximumLength = 100;

    private GameId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<GameId, DomainError> Create(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainError.BadRequest(
                "Game id is required.",
                code: DomainErrorCodes.InvalidGameId);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumLength)
        {
            return DomainError.Validation(
                $"Game id cannot exceed {MaximumLength} characters.",
                code: DomainErrorCodes.InvalidGameId);
        }

        return new GameId(normalizedValue);
    }

    public override string ToString() => Value;
}
