// Responsabilidad del archivo: Agrupa parámetros, proyección y especificación segura del listado.
// Relación en el sistema: API entrega texto sin validar; el handler produce enums que Infrastructure puede convertir a SQL seguro.
namespace DotNetTestMundial.Application.Teams.GetTeams;

/// <summary>Raw query parameters received by the API and validated by its handler.</summary>
public sealed record GetTeamsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "name",
    string SortDirection = "asc");

/// <summary>Projection returned from SQL; Domain entities are not materialized for reads.</summary>
public sealed record TeamListItem(Guid Id, string Name, string ShortName);

public enum TeamSortField { Id, Name, ShortName }
public enum QuerySortDirection { Ascending, Descending }

/// <summary>
/// Validated values passed to Infrastructure. Enums prevent arbitrary client text from
/// ever becoming an ORDER BY fragment.
/// </summary>
public sealed record TeamPageSpecification(
    string? Search,
    int PageNumber,
    int PageSize,
    TeamSortField SortField,
    QuerySortDirection SortDirection);
