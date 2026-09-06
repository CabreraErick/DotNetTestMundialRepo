using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default);

    // Discards pending changes. It cannot undo an already committed transaction.
    void Rollback();
}
