// Responsabilidad del archivo: Comparte el mapeo de identidad y eventos no persistidos.
// Relación en el sistema: Las configuraciones de todas las entidades reutilizan estas reglas EF Core.
using DotNetTestMundial.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetTestMundial.Infrastructure.Persistence.Configurations;

internal static class EntityConfiguration
{
    public static void ConfigureIdentity<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : Entity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Ignore(x => x.DomainEvents);
    }
}
