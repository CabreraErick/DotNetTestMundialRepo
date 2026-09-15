// Startup schema/seed operations are not business Commands. All business writes
// continue to use IUnitOfWork. Reuse the versioned seed rather than duplicate data.
using Dapper;
using DotNetTestMundial.Infrastructure.Persistence.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DotNetTestMundial.Infrastructure.Persistence;

public sealed class TournamentDatabaseInitializer(TournamentDbContext context, string connectionString)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
        await using var connection = new SqlConnection(connectionString);
        var hasTeams = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Teams) THEN 1 ELSE 0 END AS bit)",
            cancellationToken: cancellationToken));
        if (hasTeams) return;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var operation in new SeedWorldCup2026Tournament().UpOperations.OfType<SqlOperation>())
            await context.Database.ExecuteSqlRawAsync(operation.Sql, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
