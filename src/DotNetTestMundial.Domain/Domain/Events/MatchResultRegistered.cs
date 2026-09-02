namespace DotNetTestMundial.Domain.Events;

public sealed record MatchResultRegisteredEvent(
    Guid MatchId,
    int HomeScore,
    int AwayScore,
    DateTime OccurredAt);