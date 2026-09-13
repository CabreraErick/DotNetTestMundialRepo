// Responsabilidad del archivo: Centraliza errores esperados de paginación y orden de jugadores.
// Relación en el sistema: El Query Handler los devuelve antes de abrir una conexión y API los presenta como HTTP 400.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Players.GetPlayers;

public static class GetPlayersErrors
{
    public static readonly Error InvalidTeamId = new(
        "Players.InvalidTeamId", "TeamId cannot be an empty identifier.", ErrorType.Validation);
    public static readonly Error InvalidPageNumber = new(
        "Players.InvalidPageNumber", "PageNumber must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidPageSize = new(
        "Players.InvalidPageSize", "PageSize must be between 1 and 100.", ErrorType.Validation);
    public static readonly Error InvalidSortBy = new(
        "Players.InvalidSortBy", "SortBy must be id, teamId, name, jerseyNumber or isActive.", ErrorType.Validation);
    public static readonly Error InvalidSortDirection = new(
        "Players.InvalidSortDirection", "SortDirection must be asc or desc.", ErrorType.Validation);
}
