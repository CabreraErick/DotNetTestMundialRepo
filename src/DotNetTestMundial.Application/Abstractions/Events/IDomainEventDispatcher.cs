// Responsabilidad del archivo: Declara el puerto que procesa eventos producidos por las entidades.
// Relación en el sistema: UnitOfWork entrega eventos confirmados y Infrastructure decide cómo observarlos.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
