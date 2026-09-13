// Responsabilidad del archivo: Describe el hecho de que un equipo fue creado.
// Relación en el sistema: Team lo emite; la capa de observabilidad futura lo registrará después del commit.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Events;

public sealed record TeamCreatedEvent(
    Guid TeamId,
    string TeamName,
    DateTime OccurredAt) : IDomainEvent;
