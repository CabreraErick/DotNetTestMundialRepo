// Responsabilidad del archivo: Agrupa parámetros, proyección y valores seguros para consultar jugadores.
// Relación en el sistema: API recibe filtros; Application los valida e Infrastructure los transforma en SQL Dapper parametrizado.
namespace DotNetTestMundial.Application.Players.GetPlayers;

public sealed record GetPlayersQuery(
    string? Search = null,
    Guid? TeamId = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "name",
    string SortDirection = "asc");

public sealed record PlayerListItem(
    Guid Id,
    Guid TeamId,
    string Name,
    int JerseyNumber,
    bool IsActive);

public enum PlayerSortField { Id, TeamId, Name, JerseyNumber, IsActive }
public enum PlayerSortDirection { Ascending, Descending }

public sealed record PlayerPageSpecification(
    string? Search,
    Guid? TeamId,
    bool? IsActive,
    int PageNumber,
    int PageSize,
    PlayerSortField SortField,
    PlayerSortDirection SortDirection);
