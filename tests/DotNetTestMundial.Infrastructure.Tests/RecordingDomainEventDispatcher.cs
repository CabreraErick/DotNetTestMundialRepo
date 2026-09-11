// Responsabilidad del archivo: Captura eventos enviados por UnitOfWork durante pruebas.
// Relación en el sistema: Sustituye el adaptador de logging para observar el contrato sin depender de la consola.
using DotNetTestMundial.Application.Abstractions.Events;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Infrastructure.Tests;

internal sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<IDomainEvent> _events = new();

    public IReadOnlyCollection<IDomainEvent> Events => _events.AsReadOnly();

    public Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.AddRange(domainEvents);
        return Task.CompletedTask;
    }
}
