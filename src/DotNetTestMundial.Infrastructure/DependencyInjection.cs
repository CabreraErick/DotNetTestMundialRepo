// Responsabilidad del archivo: Registra adaptadores de persistencia y sus ciclos de vida.
// Relación en el sistema: API llama AddInfrastructure y los handlers reciben interfaces definidas por Application.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Infrastructure.Persistence;
using DotNetTestMundial.Infrastructure.Persistence.Repositories;
using DotNetTestMundial.Infrastructure.Persistence.Idempotency;
using DotNetTestMundial.Infrastructure.Persistence.Queries;
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
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyStore>(provider => new SqlIdempotencyStore(
            provider.GetRequiredService<TournamentDbContext>(), connectionString));
        // Read repositories receive only the connection string and use Dapper; they never
        // resolve TournamentDbContext, preserving the CQRS read/write separation.
        services.AddScoped<ITeamReadRepository>(_ => new SqlTeamReadRepository(connectionString));
        services.AddScoped<IPlayerReadRepository>(_ => new SqlPlayerReadRepository(connectionString));
        services.AddSingleton<IPersistenceErrorTranslator, SqlServerPersistenceErrorTranslator>();
        return services;
    }
}
