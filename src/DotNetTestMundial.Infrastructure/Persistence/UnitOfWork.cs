using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DotNetTestMundial.Infrastructure.Persistence;

public sealed class UnitOfWork(TournamentDbContext context, IPersistenceErrorTranslator errorTranslator) : IUnitOfWork
{
    public async Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var affectedEntries = await context.SaveFromUnitOfWorkAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            context.ChangeTracker.AcceptAllChanges();
            return Result<int>.Success(affectedEntries);
        }
        catch (DbUpdateException exception)
        {
            await AbortAsync(transaction);
            var error = errorTranslator.Translate(exception);
            if (error is not null)
                return Result<int>.Failure(error);
            throw;
        }
        catch
        {
            await AbortAsync(transaction);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public void Rollback() => context.ChangeTracker.Clear();

    private async Task AbortAsync(IDbContextTransaction? transaction)
    {
        try
        {
            // Cleanup must still run when the request's cancellation token is cancelled.
            if (transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            Rollback();
        }
    }
}
