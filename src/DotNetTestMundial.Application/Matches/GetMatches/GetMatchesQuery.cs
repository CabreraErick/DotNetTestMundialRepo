// Responsabilidad del archivo: Agrupa filtros, proyección y especificación segura del calendario de partidos.
// Relación en el sistema: API recibe valores simples, Application los valida e Infrastructure los ejecuta con Dapper.
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Matches.GetMatches;

public sealed record GetMatchesQuery(
    Guid? TeamId = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "scheduledAt",
    string SortDirection = "asc");

public sealed record MatchListItem(
    Guid Id,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    DateTime ScheduledAt,
    MatchStatus Status,
    int? HomeScore,
    int? AwayScore,
    long GoalCount);

public enum MatchSortField { Id, HomeTeamId, AwayTeamId, ScheduledAt, Status }
public enum MatchSortDirection { Ascending, Descending }

public sealed record MatchPageSpecification(
    Guid? TeamId,
    MatchStatus? Status,
    DateTime? From,
    DateTime? To,
    int PageNumber,
    int PageSize,
    MatchSortField SortField,
    MatchSortDirection SortDirection);
