using Rovio.Matchmaking.Domain.Entities;

namespace Rovio.Matchmaking.Domain.Services;

public sealed class MatchingService
{
    public Result<IReadOnlyList<MatchTicket>, DomainError> SelectMatchGroup(
        IReadOnlyList<MatchTicket> candidates,
        GameMatchConfig config,
        MatchRegion region,
        DateTimeOffset now)
    {
        if (candidates is null)
        {
            return DomainError.Validation(
                "Match candidates are required.",
                code: DomainErrorCodes.InvalidMatchingInput);
        }

        if (config is null)
        {
            return DomainError.Validation(
                "Game match config is required.",
                code: DomainErrorCodes.InvalidMatchingInput);
        }

        if (region is null)
        {
            return DomainError.Validation(
                "Match region is required.",
                code: DomainErrorCodes.InvalidMatchingInput);
        }

        var ordered = candidates
            .Where(ticket => ticket.Status == TicketStatus.Queued)
            .OrderBy(ticket => ticket.EnqueuedAt)
            .ThenBy(ticket => ticket.Id.Value)
            .Select(ticket => new Candidate(
                ticket,
                config.LatencyPolicy.MaximumAcceptableDelta(ticket.WaitTime(now))))
            .ToList();

        var invariantError = ValidateShardInvariants(ordered, config, region);
        if (invariantError is not null)
        {
            return invariantError;
        }

        if (!config.PlayerCapacity.CanStart(ordered.Count))
        {
            return Array.Empty<MatchTicket>();
        }

        for (var seedIndex = 0; seedIndex < ordered.Count; seedIndex++)
        {
            var seed = ordered[seedIndex];
            var compatible = new List<Candidate> { seed };

            for (var candidateIndex = 0;
                 candidateIndex < ordered.Count &&
                 config.PlayerCapacity.CanAccept(compatible.Count);
                 candidateIndex++)
            {
                if (candidateIndex == seedIndex)
                {
                    continue;
                }

                var candidate = ordered[candidateIndex];
                if (IsCompatibleWithGroup(candidate, compatible))
                {
                    compatible.Add(candidate);
                }
            }

            if (config.PlayerCapacity.CanStart(compatible.Count))
            {
                return compatible
                    .OrderBy(candidate => candidate.Ticket.EnqueuedAt)
                    .ThenBy(candidate => candidate.Ticket.Id.Value)
                    .Select(candidate => candidate.Ticket)
                    .ToList();
            }
        }

        return Array.Empty<MatchTicket>();
    }

    private static DomainError? ValidateShardInvariants(
        IReadOnlyList<Candidate> ordered,
        GameMatchConfig config,
        MatchRegion region)
    {
        foreach (var candidate in ordered)
        {
            if (candidate.Ticket.GameId != config.GameId ||
                !candidate.Ticket.Region.IsSameAs(region))
            {
                return DomainError.Conflict(
                    "Match candidates must belong to the same game and region shard.",
                    DomainErrorCodes.MismatchedMatchShard);
            }
        }

        if (ordered
            .GroupBy(candidate => candidate.Ticket.PlayerId)
            .Any(group => group.Count() > 1))
        {
            return DomainError.Conflict(
                "Match candidates contain duplicate players for the same shard.",
                DomainErrorCodes.DuplicateQueuedPlayer);
        }

        return null;
    }

    private static bool IsCompatibleWithGroup(
        Candidate candidate,
        IReadOnlyCollection<Candidate> currentGroup) =>
        currentGroup.All(existing =>
        {
            var allowedDelta = LatencyDelta.Min(
                candidate.MaximumDelta,
                existing.MaximumDelta);

            var actualDelta = candidate.Ticket.Latency.DifferenceFrom(
                existing.Ticket.Latency);

            return allowedDelta.Allows(actualDelta);
        });

    private sealed record Candidate(
        MatchTicket Ticket,
        LatencyDelta MaximumDelta);
}
