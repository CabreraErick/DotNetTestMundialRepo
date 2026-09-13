// Responsabilidad del archivo: Representa la sesión EF Core de escritura del torneo.
// Relación en el sistema: Aplica configuraciones y restringe SaveChanges para obligar el uso de Unit of Work.
using Microsoft.EntityFrameworkCore;

namespace DotNetTestMundial.Infrastructure.Persistence;

public sealed class TournamentDbContext(DbContextOptions<TournamentDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TournamentDbContext).Assembly);
    }

    // Only UnitOfWork uses this entry point. Accept states after the DB commit succeeds.
    internal Task<int> SaveFromUnitOfWorkAsync(CancellationToken cancellationToken) =>
        base.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);

    public override int SaveChanges() => throw DirectSaveNotAllowed();
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw DirectSaveNotAllowed();
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw DirectSaveNotAllowed();
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw DirectSaveNotAllowed();

    private static InvalidOperationException DirectSaveNotAllowed() =>
        new("Use IUnitOfWork.CommitAsync to save changes.");
}
