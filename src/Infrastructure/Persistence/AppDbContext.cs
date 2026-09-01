using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Jugador> Jugadores { get; set; }
        public DbSet<Partido> Partidos { get; set; }
        public DbSet<Resultado> Resultados { get; set; }
        public DbSet<Gol> Goles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relaciones
            modelBuilder.Entity<Equipo>()
                .HasMany(e => e.Jugadores)
                .WithOne(j => j.Equipo)
                .HasForeignKey(j => j.EquipoId);

            modelBuilder.Entity<Partido>()
                .HasOne(p => p.Resultado)
                .WithOne(r => r.Partido)
                .HasForeignKey<Resultado>(r => r.PartidoId);

            modelBuilder.Entity<Gol>()
                .HasOne(g => g.Jugador)
                .WithMany()
                .HasForeignKey(g => g.JugadorId);

            modelBuilder.Entity<Gol>()
                .HasOne(g => g.Partido)
                .WithMany()
                .HasForeignKey(g => g.PartidoId);
        }
    }
}
