// Responsabilidad del archivo: Implementa el commit explícito, rollback y despacho posterior de eventos.
// Relación en el sistema: Agrupa cambios de repositorios, traduce fallos y entrega eventos confirmados al puerto de Application.
using DotNetTestMundial.Application.Abstractions.Events;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DotNetTestMundial.Infrastructure.Persistence;

public sealed class UnitOfWork(
    TournamentDbContext context,
    IPersistenceErrorTranslator errorTranslator,
    IDomainEventDispatcher domainEventDispatcher) : IUnitOfWork
{
    public async Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = GetTrackedDomainEntities();
        var domainEvents = domainEntities.SelectMany(entity => entity.DomainEvents).ToArray();
        var persistenceResult = await CommitTransactionAsync(cancellationToken);

        if (persistenceResult.IsFailure)
            return persistenceResult;

        // Events describe facts already committed to the database. Request cancellation
        // cannot reverse those facts, so observation completes with a cleanup-safe token.
        try
        {
            await domainEventDispatcher.DispatchAsync(domainEvents, CancellationToken.None);
        }
        finally
        {
            foreach (var entity in domainEntities)
                entity.ClearDomainEvents();
        }

        return persistenceResult;
    }

    public void Rollback()
    {
        foreach (var entity in GetTrackedDomainEntities())
            entity.ClearDomainEvents();

        context.ChangeTracker.Clear();
    }

    private async Task<Result<int>> CommitTransactionAsync(CancellationToken cancellationToken)
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

    private Entity[] GetTrackedDomainEntities() =>
        context.ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity)
            .Distinct()
            .ToArray();

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
