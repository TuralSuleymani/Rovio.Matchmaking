namespace Rovio.Matchmaking.Domain.ValueObjects;

public sealed record PlayerId
{
    public const int MaximumLength = 128;

    private PlayerId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PlayerId, DomainError> Create(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainError.BadRequest(
                "Player id is required.",
                code: DomainErrorCodes.InvalidPlayerId);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumLength)
        {
            return DomainError.Validation(
                $"Player id cannot exceed {MaximumLength} characters.",
                code: DomainErrorCodes.InvalidPlayerId);
        }

        return new PlayerId(normalizedValue);
    }

    public override string ToString() => Value;
}
