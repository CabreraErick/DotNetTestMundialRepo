using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Infrastructure.Persistence;
using DotNetTestMundial.Infrastructure.Persistence.Repositories;
using DotNetTestMundial.Infrastructure.Persistence.Idempotency;
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
        services.AddSingleton<IPersistenceErrorTranslator, SqlServerPersistenceErrorTranslator>();
        return services;
    }
}
