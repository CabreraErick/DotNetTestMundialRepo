// Responsabilidad del archivo: Define contratos de goles, estado completo y registro del marcador.
// Relación en el sistema: Dapper llena las proyecciones, los handlers reconstruyen Domain y API publica sus respuestas.
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Matches.Results;

public sealed record GoalListItem(
    Guid Id,
    Guid MatchId,
    Guid PlayerId,
    string PlayerName,
    Guid TeamId,
    string TeamName,
    int Minute);

public enum GoalSortField { Minute, PlayerName, TeamName, Id }

public sealed record GoalPageSpecification(
    Guid MatchId, Guid HomeTeamId, Guid AwayTeamId, Guid? TeamId, string? Search,
    int PageNumber, int PageSize, GoalSortField SortField, MatchSortDirection SortDirection);

// Score totals describe the entire match, independently of filters and page size.
public sealed record MatchGoalsPage(
    IReadOnlyList<GoalListItem> Data, int PageNumber, int PageSize,
    long TotalRecords, int TotalPages, long HomeGoals, long AwayGoals)
{
    public static MatchGoalsPage Create(IReadOnlyList<GoalListItem> data, int pageNumber,
        int pageSize, long totalRecords, long homeGoals, long awayGoals)
    {
        var page = PagedResult<GoalListItem>.Create(data, pageNumber, pageSize, totalRecords);
        return new(page.Data, page.PageNumber, page.PageSize, page.TotalRecords,
            page.TotalPages, homeGoals, awayGoals);
    }
}

public sealed record MatchStateSnapshot(
    MatchListItem Match,
    IReadOnlyList<GoalListItem> Goals);

public sealed record RegisterMatchResultCommand(Guid MatchId, int HomeScore, int AwayScore);

public sealed record MatchResultOutcome(
    Guid MatchId, MatchStatus Status, int HomeScore, int AwayScore);

public static class MatchResultErrors
{
    public static readonly Error PlayerNotFound = new(
        "Goals.PlayerNotFound", "The scorer does not exist.", ErrorType.NotFound);
}
