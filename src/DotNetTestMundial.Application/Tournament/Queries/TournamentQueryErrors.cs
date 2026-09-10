// Responsabilidad del archivo: Centraliza errores de validación para las consultas estadísticas.
// Relación en el sistema: Los handlers evitan abrir Dapper con paginación u ordenamiento inválidos y API devuelve HTTP 400.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Tournament.Queries;

public static class TournamentQueryErrors
{
    public static readonly Error InvalidPageNumber = new(
        "Tournament.InvalidPageNumber", "PageNumber must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidPageSize = new(
        "Tournament.InvalidPageSize", "PageSize must be between 1 and 100.", ErrorType.Validation);
    public static readonly Error InvalidStandingSortBy = new(
        "Standings.InvalidSortBy",
        "SortBy must be points, goalDifference, goalsFor, played, won or teamName.",
        ErrorType.Validation);
    public static readonly Error InvalidScorerSortBy = new(
        "Scorers.InvalidSortBy", "SortBy must be goals, playerName or teamName.",
        ErrorType.Validation);
    public static readonly Error InvalidSortDirection = new(
        "Tournament.InvalidSortDirection", "SortDirection must be asc or desc.",
        ErrorType.Validation);
}
