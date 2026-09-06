using DotNetTestMundial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetTestMundial.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches", table =>
        {
            table.HasCheckConstraint("CK_Matches_DifferentTeams", "[HomeTeamId] <> [AwayTeamId]");
            table.HasCheckConstraint("CK_Matches_StateAndScore",
                "([Status] = 2 AND [HomeScore] IS NOT NULL AND [AwayScore] IS NOT NULL AND [HomeScore] >= 0 AND [AwayScore] >= 0) OR " +
                "([Status] IN (1, 3) AND [HomeScore] IS NULL AND [AwayScore] IS NULL)");
        });
        builder.ConfigureIdentity();
        builder.HasOne<Team>().WithMany().HasForeignKey(x => x.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Team>().WithMany().HasForeignKey(x => x.AwayTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Goals).WithOne().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Goals).HasField("_goals").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.ScheduledAt);
        builder.HasIndex(x => new { x.Status, x.ScheduledAt });
    }
}
