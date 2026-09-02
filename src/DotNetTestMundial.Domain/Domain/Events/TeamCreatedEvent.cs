namespace DotNetTestMundial.Domain.Events;

public sealed record TeamCreatedEvent(
    Guid TeamId,
    string TeamName,
    DateTime OccurredAt);