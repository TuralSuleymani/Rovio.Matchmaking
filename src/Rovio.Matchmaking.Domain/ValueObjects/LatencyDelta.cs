namespace Rovio.Matchmaking.Domain.ValueObjects
{
    public readonly record struct LatencyDelta
    {
        private LatencyDelta(int milliseconds)
        {
            Milliseconds = milliseconds;
        }

        public int Milliseconds { get; }

        public static Result<LatencyDelta, DomainError> Create(
            int milliseconds)
        {
            if (milliseconds < 0)
            {
                return DomainError.Validation(
                    "Latency delta cannot be negative.",
                    code: DomainErrorCodes.InvalidLatencyDelta);
            }

            return new LatencyDelta(milliseconds);
        }

        internal static LatencyDelta FromValidated(int milliseconds)
        {
            if (milliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(milliseconds),
                    milliseconds,
                    "Latency delta cannot be negative.");
            }

            return new LatencyDelta(milliseconds);
        }

        public bool Allows(LatencyDelta actualDelta) =>
            actualDelta.Milliseconds <= Milliseconds;

        public Result<LatencyDelta, DomainError> Add(
            LatencyDelta other)
        {
            var result =
                (long)Milliseconds + other.Milliseconds;

            if (result > int.MaxValue)
            {
                return DomainError.Validation(
                    "The resulting latency delta exceeds the supported range.",
                    code: DomainErrorCodes.LatencyDeltaOverflow);
            }

            return FromValidated((int)result);
        }

        public Result<LatencyDelta, DomainError> MultiplyBy(
            int multiplier)
        {
            if (multiplier < 0)
            {
                return DomainError.Validation(
                    "Latency delta multiplier cannot be negative.",
                    code: DomainErrorCodes.InvalidLatencyDeltaMultiplier);
            }

            var result =
                (long)Milliseconds * multiplier;

            if (result > int.MaxValue)
            {
                return DomainError.Validation(
                    "The resulting latency delta exceeds the supported range.",
                    code: DomainErrorCodes.LatencyDeltaOverflow);
            }

            return FromValidated((int)result);
        }

        public static LatencyDelta Min(
            LatencyDelta first,
            LatencyDelta second) =>
            first.Milliseconds <= second.Milliseconds
                ? first
                : second;

        public TimeSpan ToTimeSpan() =>
            TimeSpan.FromMilliseconds(Milliseconds);

        public override string ToString() =>
            $"{Milliseconds} ms";
    }
}
