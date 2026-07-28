namespace Rovio.Matchmaking.Domain.ValueObjects
{
    public sealed record MatchRegion
    {
        public const int MaximumLength = 50;

        private MatchRegion(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Result<MatchRegion, DomainError> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DomainError.Validation(
                    "Match region is required.",
                    code: DomainErrorCodes.InvalidMatchRegion);
            }

            var normalizedValue = value
                .Trim()
                .ToLowerInvariant();

            if (normalizedValue.Length > MaximumLength)
            {
                return DomainError.Validation(
                    $"Match region cannot exceed {MaximumLength} characters.",
                    code: DomainErrorCodes.InvalidMatchRegion);
            }

            return new MatchRegion(normalizedValue);
        }

        public bool IsSameAs(MatchRegion other) =>
            this == other;

        public override string ToString() => Value;
    }
}
