// Responsabilidad del archivo: Comprueba contratos y configuración de la capa Infrastructure.
// Relación en el sistema: Valida SaveChanges bloqueado, DI, esquema SQL Server y sincronía de migraciones.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Infrastructure.Persistence;
using DotNetTestMundial.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetTestMundial.Infrastructure.Tests;

public class PersistenceUnitTests
{
    private static TournamentDbContext CreateContext() => new(new DbContextOptionsBuilder<TournamentDbContext>()
        .UseSqlServer("Server=unused;Database=NotOpened;Integrated Security=true;TrustServerCertificate=true").Options);

    [Fact]
    public async Task DbContext_BlocksEveryDirectSaveEntryPoint()
    {
        await using var context = CreateContext();
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges(false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(false));
    }

    [Fact]
    public void Repositories_OnlyPrepareChangesWithoutOpeningConnection()
    {
        using var context = CreateContext();
        var team = Team.Create("Local", "LOC").Value;
        var player = Player.Create(team.Id, "Ana", 10).Value;
        new WriteRepository<Team>(context).Add(team);
        new WriteRepository<Player>(context).Add(player);
        Assert.Equal(EntityState.Added, context.Entry(team).State);
        Assert.Equal(EntityState.Added, context.Entry(player).State);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void Rollback_DiscardsAllPendingRepositoryChanges()
    {
        using var context = CreateContext();
        new WriteRepository<Team>(context).Add(Team.Create("Local", "LOC").Value);
        var unitOfWork = SqliteTestDatabase.CreateUnitOfWork(context);
        unitOfWork.Rollback();
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task CancelledCommit_DiscardsChangesWithoutConnecting()
    {
        await using var context = CreateContext();
        new WriteRepository<Team>(context).Add(Team.Create("Local", "LOC").Value);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var unitOfWork = SqliteTestDatabase.CreateUnitOfWork(context);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => unitOfWork.CommitAsync(cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void ErrorTranslator_DoesNotDisguiseTechnicalFailuresAsConflicts()
    {
        var translator = new SqlServerPersistenceErrorTranslator();
        Assert.Null(translator.Translate(new DbUpdateException("Connection failed")));
        Assert.Equal(PersistenceErrors.ConcurrentChange, translator.Translate(new DbUpdateConcurrencyException()));
    }

    [Fact]
    public void DependencyInjection_SharesContextWithinScopeAndIsolatesScopes()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Server=unused;Database=NotOpened;Integrated Security=true");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var team = Team.Create("Local", "LOC").Value;
        first.ServiceProvider.GetRequiredService<IWriteRepository<Team>>().Add(team);
        Assert.Same(team, Assert.Single(first.ServiceProvider.GetRequiredService<TournamentDbContext>().ChangeTracker.Entries<Team>()).Entity);
        Assert.Empty(second.ServiceProvider.GetRequiredService<TournamentDbContext>().ChangeTracker.Entries());
        Assert.IsType<UnitOfWork>(first.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public void SqlServerModel_GeneratesSchemaWithoutConnecting()
    {
        using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE [Teams]", script);
        Assert.Contains("CREATE TABLE [Players]", script);
        Assert.Contains("CREATE TABLE [Matches]", script);
        Assert.Contains("CREATE TABLE [Goals]", script);
        Assert.Contains("CREATE TABLE [IdempotencyRecords]", script);
        Assert.DoesNotContain("DomainEvents", script);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void Migrations_MatchCurrentSqlServerModelAndIncludeSeed()
    {
        using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();
        Assert.Equal(3, migrations.Length);
        Assert.EndsWith("_SeedWorldCup2026Tournament", migrations[^1]);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
