// Responsabilidad del archivo: Define el contrato común para casos de uso que modifican estado.
// Relación en el sistema: Los controladores invocan handlers y éstos coordinan dominio, repositorios y Unit of Work.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
