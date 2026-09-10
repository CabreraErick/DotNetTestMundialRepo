// Responsabilidad del archivo: Agrupa solicitudes, proyecciones y especificaciones seguras de las estadísticas del torneo.
// Relación en el sistema: API recibe parámetros simples, los handlers los validan y el repositorio Dapper ejecuta los enums resultantes.
namespace DotNetTestMundial.Application.Tournament.Queries;

public sealed record GetStandingsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "points",
    string SortDirection = "desc");

public sealed record StandingListItem(
    long Position,
    Guid TeamId,
    string TeamName,
    string ShortName,
    long Played,
    long Won,
    long Drawn,
    long Lost,
    long GoalsFor,
    long GoalsAgainst,
    long GoalDifference,
    long Points);

public enum StandingSortField
{
    Points,
    GoalDifference,
    GoalsFor,
    Played,
    Won,
    TeamName
}

public sealed record StandingPageSpecification(
    string? Search,
    int PageNumber,
    int PageSize,
    StandingSortField SortField,
    TournamentSortDirection SortDirection);

public sealed record GetScorersQuery(
    string? Search = null,
    Guid? TeamId = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "goals",
    string SortDirection = "desc");

public sealed record ScorerListItem(
    long Position,
    Guid PlayerId,
    string PlayerName,
    Guid TeamId,
    string TeamName,
    long Goals);

public enum ScorerSortField
{
    Goals,
    PlayerName,
    TeamName
}

public enum TournamentSortDirection
{
    Ascending,
    Descending
}

public sealed record ScorerPageSpecification(
    string? Search,
    Guid? TeamId,
    int PageNumber,
    int PageSize,
    ScorerSortField SortField,
    TournamentSortDirection SortDirection);
