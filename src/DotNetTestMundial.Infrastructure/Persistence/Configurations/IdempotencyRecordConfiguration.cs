// Responsabilidad del archivo: Configura con EF Core la persistencia de IdempotencyRecord.
// Relación en el sistema: TournamentDbContext descubre este mapeo y las migraciones reflejan sus restricciones.
using DotNetTestMundial.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetTestMundial.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(x => new { x.Operation, x.Key });
        builder.Property(x => x.Operation).HasMaxLength(100);
        builder.Property(x => x.Key).HasMaxLength(200);
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsFixedLength();
        builder.Property(x => x.ResponseBody).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
    }
}
