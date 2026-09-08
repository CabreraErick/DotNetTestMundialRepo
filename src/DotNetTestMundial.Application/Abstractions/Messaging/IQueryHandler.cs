// Responsabilidad del archivo: Define el contrato común para casos de uso de sólo lectura.
// Relación en el sistema: Separa las Queries Dapper de los Commands EF Core dentro de CQRS.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Messaging;

/// <summary>
/// Represents an Application use case that only reads data. Implementations must not
/// call Unit of Work because the architecture reserves EF Core and commits for Commands.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
