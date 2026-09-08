// Responsabilidad del archivo: Centraliza errores de paginación y ordenamiento de equipos.
// Relación en el sistema: Evita abrir la conexión cuando los parámetros HTTP son inválidos.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Teams.GetTeams;

/// <summary>Expected query-string validation errors, later translated to HTTP 400.</summary>
public static class GetTeamsErrors
{
    public static readonly Error InvalidPageNumber = new(
        "Teams.InvalidPageNumber", "PageNumber must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidPageSize = new(
        "Teams.InvalidPageSize", "PageSize must be between 1 and 100.", ErrorType.Validation);
    public static readonly Error InvalidSortBy = new(
        "Teams.InvalidSortBy", "SortBy must be id, name or shortName.", ErrorType.Validation);
    public static readonly Error InvalidSortDirection = new(
        "Teams.InvalidSortDirection", "SortDirection must be asc or desc.", ErrorType.Validation);
}
