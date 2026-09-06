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
