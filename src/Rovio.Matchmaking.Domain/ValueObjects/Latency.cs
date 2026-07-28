namespace Rovio.Matchmaking.Domain.ValueObjects
{
    public readonly record struct Latency
    {
        private Latency(int milliseconds)
        {
            Milliseconds = milliseconds;
        }

        public int Milliseconds { get; }

        public static Result<Latency, DomainError> Create(int milliseconds)
        {
            if (milliseconds < 0)
            {
                return DomainError.Validation(
                    "Latency cannot be negative.",
                    code: DomainErrorCodes.InvalidLatency);
            }

            return new Latency(milliseconds);
        }

        public LatencyDelta DifferenceFrom(Latency other)
        {
            var difference = Math.Abs(
                (long)Milliseconds - other.Milliseconds);

            return LatencyDelta.FromValidated((int)difference);
        }

        public bool IsWithin(Latency maximumLatency) =>
            Milliseconds <= maximumLatency.Milliseconds;

        public TimeSpan ToTimeSpan() =>
            TimeSpan.FromMilliseconds(Milliseconds);

        public override string ToString() =>
            $"{Milliseconds} ms";
    }
}
