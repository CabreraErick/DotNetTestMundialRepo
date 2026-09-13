// Responsabilidad del archivo: Proporciona una base SQLite relacional aislada para pruebas.
// Relación en el sistema: Infrastructure.Tests la usa para comprobar transacciones sin tocar SQL Server del usuario.
using DotNetTestMundial.Application.Abstractions.Events;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DotNetTestMundial.Infrastructure.Tests;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteTestDatabase(SqliteConnection connection) => _connection = connection;

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var database = new SqliteTestDatabase(connection);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync();
        return database;
    }

    public TournamentDbContext CreateContext(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<TournamentDbContext>().UseSqlite(_connection)
            .AddInterceptors(interceptors).Options);

    public static UnitOfWork CreateUnitOfWork(
        TournamentDbContext context,
        IDomainEventDispatcher? dispatcher = null) =>
        new(context, new SqliteErrorTranslator(), dispatcher ?? new RecordingDomainEventDispatcher());

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    // SQL Server is the production provider. This translator handles test-provider codes only.
    private sealed class SqliteErrorTranslator : IPersistenceErrorTranslator
    {
        public Error? Translate(DbUpdateException exception) => exception switch
        {
            DbUpdateConcurrencyException => PersistenceErrors.ConcurrentChange,
            { InnerException: SqliteException { SqliteErrorCode: 19 } } => PersistenceErrors.ConstraintViolation,
            _ => null
        };
    }
}
