// Responsabilidad del archivo: Crea el DbContext para herramientas de diseño de EF Core.
// Relación en el sistema: dotnet-ef la usa para generar y aplicar migraciones sin iniciar API.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetTestMundial.Infrastructure.Persistence;

// Tooling uses Infrastructure directly, without starting the API or connecting on creation.
public sealed class TournamentDbContextFactory : IDesignTimeDbContextFactory<TournamentDbContext>
{
    public TournamentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Tournament");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set ConnectionStrings__Tournament to the dedicated database connection before using EF tools.");

        return new TournamentDbContext(new DbContextOptionsBuilder<TournamentDbContext>()
            .UseSqlServer(connectionString).Options);
    }
}
