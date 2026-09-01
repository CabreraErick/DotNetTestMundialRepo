using Domain.Entities;

namespace Infrastructure.Persistence
{
    public static class SeedData
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            // Si ya existen equipos, no se ejecuta el seed
            if (context.Equipos.Any()) return;

            // 🔹 Equipos
            var equipos = new List<Equipo>
            {
                new Equipo { Nombre = "Tigres", Representante = "Carlos Pérez" },
                new Equipo { Nombre = "Leones", Representante = "Ana Gómez" },
                new Equipo { Nombre = "Águilas", Representante = "Luis Martínez" },
                new Equipo { Nombre = "Toros", Representante = "María López" }
            };
            context.Equipos.AddRange(equipos);
            await context.SaveChangesAsync();

            // 🔹 Jugadores (5 por equipo)
            foreach (var equipo in equipos)
            {
                for (int i = 1; i <= 5; i++)
                {
                    context.Jugadores.Add(new Jugador
                    {
                        Nombre = $"Jugador {i} {equipo.Nombre}",
                        NumeroCamiseta = i,
                        EquipoId = equipo.Id
                    });
                }
            }
            await context.SaveChangesAsync();

            // 🔹 Partidos (6 en total, 3 con resultados)
            var partidos = new List<Partido>
            {
                new Partido { EquipoLocalId = equipos[0].Id, EquipoVisitanteId = equipos[1].Id, Fecha = DateTime.Now.AddDays(-7), Estado = "Finalizado" },
                new Partido { EquipoLocalId = equipos[2].Id, EquipoVisitanteId = equipos[3].Id, Fecha = DateTime.Now.AddDays(-6), Estado = "Finalizado" },
                new Partido { EquipoLocalId = equipos[0].Id, EquipoVisitanteId = equipos[2].Id, Fecha = DateTime.Now.AddDays(-5), Estado = "Finalizado" },
                new Partido { EquipoLocalId = equipos[1].Id, EquipoVisitanteId = equipos[3].Id, Fecha = DateTime.Now.AddDays(2), Estado = "Programado" },
                new Partido { EquipoLocalId = equipos[0].Id, EquipoVisitanteId = equipos[3].Id, Fecha = DateTime.Now.AddDays(3), Estado = "Programado" },
                new Partido { EquipoLocalId = equipos[1].Id, EquipoVisitanteId = equipos[2].Id, Fecha = DateTime.Now.AddDays(4), Estado = "Programado" }
            };
            context.Partidos.AddRange(partidos);
            await context.SaveChangesAsync();

            // 🔹 Resultados para 3 partidos
            context.Resultados.AddRange(new List<Resultado>
            {
                new Resultado { PartidoId = partidos[0].Id, GolesLocal = 2, GolesVisitante = 1 },
                new Resultado { PartidoId = partidos[1].Id, GolesLocal = 3, GolesVisitante = 3 },
                new Resultado { PartidoId = partidos[2].Id, GolesLocal = 1, GolesVisitante = 0 }
            });
            await context.SaveChangesAsync();
        }
    }
}
