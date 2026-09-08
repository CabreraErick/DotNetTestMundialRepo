// Responsabilidad del archivo: Marca hechos de negocio ocurridos dentro del dominio.
// Relación en el sistema: Entity conserva estos eventos para su futuro procesamiento y trazabilidad.
namespace DotNetTestMundial.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
