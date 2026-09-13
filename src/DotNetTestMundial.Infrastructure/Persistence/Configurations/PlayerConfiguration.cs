// Responsabilidad del archivo: Configura con EF Core la persistencia de Player.
// Relación en el sistema: TournamentDbContext descubre este mapeo y las migraciones reflejan sus restricciones.
using DotNetTestMundial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetTestMundial.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players", table => table.HasCheckConstraint("CK_Players_JerseyNumber", "[JerseyNumber] > 0"));
        builder.ConfigureIdentity();
        builder.Property(x => x.Name).HasMaxLength(Player.MaxNameLength).IsRequired();
        // The composite key ensures a goal's stored team really belongs to its scorer.
        builder.HasAlternateKey(x => new { x.Id, x.TeamId });
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => new { x.TeamId, x.JerseyNumber })
            .IsUnique()
            .HasDatabaseName("UX_Players_TeamId_JerseyNumber");
    }
}
