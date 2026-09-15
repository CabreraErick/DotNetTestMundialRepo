// Responsabilidad del archivo: Registra de forma estructurada los eventos de dominio ya confirmados.
// Relación en el sistema: Implementa el puerto de Application y recibe los eventos desde UnitOfWork.
using DotNetTestMundial.Application.Abstractions.Events;
using DotNetTestMundial.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DotNetTestMundial.Infrastructure.Observability;

public sealed class LoggingDomainEventDispatcher(
    ILogger<LoggingDomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation(
                "Domain event {DomainEventType} occurred at {OccurredAt}. TraceId: {TraceId}. Payload: {DomainEvent}",
                domainEvent.GetType().Name,
                domainEvent.OccurredAt,
                Activity.Current?.TraceId.ToString(),
                domainEvent);
        }

        return Task.CompletedTask;
    }
}
