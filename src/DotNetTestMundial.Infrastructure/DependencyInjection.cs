// Responsabilidad del archivo: Registra adaptadores de persistencia y sus ciclos de vida.
// Relación en el sistema: API llama AddInfrastructure y los handlers reciben interfaces definidas por Application.
using DotNetTestMundial.Application.Abstractions.Events;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Infrastructure.Persistence;
using DotNetTestMundial.Infrastructure.Persistence.Repositories;
using DotNetTestMundial.Infrastructure.Persistence.Idempotency;
using DotNetTestMundial.Infrastructure.Persistence.Queries;
using DotNetTestMundial.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetTestMundial.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        // Retries must encompass the entire future Command, not an individual SaveChanges.
        services.AddDbContext<TournamentDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped(provider => new TournamentDatabaseInitializer(
            provider.GetRequiredService<TournamentDbContext>(), connectionString));
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        services.AddLogging();
        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyStore>(provider => new SqlIdempotencyStore(
            provider.GetRequiredService<TournamentDbContext>(), connectionString));
        // Read repositories receive only the connection string and use Dapper; they never
        // resolve TournamentDbContext, preserving the CQRS read/write separation.
        services.AddScoped<ITeamReadRepository>(_ => new SqlTeamReadRepository(connectionString));
        services.AddScoped<IPlayerReadRepository>(_ => new SqlPlayerReadRepository(connectionString));
        services.AddScoped<IMatchReadRepository>(_ => new SqlMatchReadRepository(connectionString));
        services.AddScoped<ITournamentReadRepository>(_ => new SqlTournamentReadRepository(connectionString));
        services.AddSingleton<IPersistenceErrorTranslator, SqlServerPersistenceErrorTranslator>();
        return services;
    }
}
