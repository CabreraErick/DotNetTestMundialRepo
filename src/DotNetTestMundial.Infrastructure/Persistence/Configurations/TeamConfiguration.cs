// Responsabilidad del archivo: Configura con EF Core la persistencia de Team.
// Relación en el sistema: TournamentDbContext descubre este mapeo y las migraciones reflejan sus restricciones.
using DotNetTestMundial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetTestMundial.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.ConfigureIdentity();
        builder.Property(x => x.Name).HasMaxLength(Team.MaxNameLength).IsRequired();
        builder.Property(x => x.ShortName).HasMaxLength(Team.MaxShortNameLength).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_Teams_Name");
        builder.HasIndex(x => x.ShortName).IsUnique().HasDatabaseName("UX_Teams_ShortName");
        builder.HasMany(x => x.Players).WithOne().HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Players).HasField("_players").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
