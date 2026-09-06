using DotNetTestMundial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetTestMundial.Infrastructure.Persistence.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals", table => table.HasCheckConstraint("CK_Goals_Minute", "[Minute] >= 1 AND [Minute] <= 120"));
        builder.ConfigureIdentity();
        builder.HasOne<Player>().WithMany().HasForeignKey(x => new { x.PlayerId, x.TeamId })
            .HasPrincipalKey(x => new { x.Id, x.TeamId }).OnDelete(DeleteBehavior.Restrict);
    }
}
