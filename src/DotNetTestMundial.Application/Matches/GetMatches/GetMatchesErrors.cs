// Responsabilidad del archivo: Centraliza errores de filtros, rango, paginación y orden del calendario.
// Relación en el sistema: GetMatchesQueryHandler los devuelve antes de abrir SQL y API los convierte en HTTP 400.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Matches.GetMatches;

public static class GetMatchesErrors
{
    public static readonly Error InvalidTeamId = new(
        "Matches.InvalidTeamId", "TeamId cannot be an empty identifier.", ErrorType.Validation);
    public static readonly Error InvalidStatus = new(
        "Matches.InvalidStatus", "Status must be Scheduled, Played or Cancelled.", ErrorType.Validation);
    public static readonly Error InvalidDateRange = new(
        "Matches.InvalidDateRange", "From cannot be later than To.", ErrorType.Validation);
    public static readonly Error InvalidPageNumber = new(
        "Matches.InvalidPageNumber", "PageNumber must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidPageSize = new(
        "Matches.InvalidPageSize", "PageSize must be between 1 and 100.", ErrorType.Validation);
    public static readonly Error InvalidSortBy = new(
        "Matches.InvalidSortBy", "SortBy must be id, homeTeamId, awayTeamId, scheduledAt or status.", ErrorType.Validation);
    public static readonly Error InvalidSortDirection = new(
        "Matches.InvalidSortDirection", "SortDirection must be asc or desc.", ErrorType.Validation);
}
