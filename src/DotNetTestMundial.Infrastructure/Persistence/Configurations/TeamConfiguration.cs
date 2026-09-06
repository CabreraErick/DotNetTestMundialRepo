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
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.ShortName).IsRequired();
        builder.HasMany(x => x.Players).WithOne().HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Players).HasField("_players").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
