// Responsabilidad del archivo: Enumera las transiciones persistibles de un partido.
// Relación en el sistema: Match controla los cambios y las consultas futuras usarán sus valores para filtros y estadísticas.
namespace DotNetTestMundial.Domain.Enums;

public enum MatchStatus
{
    Scheduled = 1,
    Played = 2,
    Cancelled = 3
}

/*
    Permite representar el estado del partido
    Proceso:
    Scheduled -> RegisterResult
        -> Played

    Scheduled -> Cancel
        -> CAncelled
*/