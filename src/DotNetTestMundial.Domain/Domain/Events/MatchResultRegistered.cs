// Responsabilidad del archivo: Describe el hecho de que un resultado fue registrado.
// Relación en el sistema: Match lo emite; la publicación futura deberá ocurrir sólo tras confirmar Unit of Work.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Events;

public sealed record MatchResultRegisteredEvent(
    Guid MatchId,
    int HomeScore,
    int AwayScore,
    DateTime OccurredAt) : IDomainEvent;
