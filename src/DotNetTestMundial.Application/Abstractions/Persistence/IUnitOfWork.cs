// Responsabilidad del archivo: Declara el límite transaccional requerido por los Commands.
// Relación en el sistema: Infrastructure lo implementa y ningún handler llama SaveChanges directamente.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default);

    // Discards pending changes. It cannot undo an already committed transaction.
    void Rollback();
}
