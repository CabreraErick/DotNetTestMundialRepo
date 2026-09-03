using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Events;

public sealed record MatchResultRegisteredEvent(
    Guid MatchId,
    int HomeScore,
    int AwayScore,
    DateTime OccurredAt) : IDomainEvent;