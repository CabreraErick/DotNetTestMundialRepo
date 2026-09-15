// Responsabilidad del archivo: Resuelve la consulta de goles de un partido existente.
// Relación en el sistema: Expone a API una proyección Dapper sin introducir EF Core en el lado de lectura CQRS.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Matches.Results;

public sealed record GetMatchGoalsQuery(Guid MatchId, Guid? TeamId = null, string? Search = null,
    int PageNumber = 1, int PageSize = 10, string SortBy = "minute", string SortDirection = "asc");

public sealed class GetMatchGoalsQueryHandler(IMatchReadRepository matches)
    : IQueryHandler<GetMatchGoalsQuery, MatchGoalsPage>
{
    public async Task<Result<MatchGoalsPage>> HandleAsync(
        GetMatchGoalsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.PageNumber <= 0)
            return Result<MatchGoalsPage>.Failure(GetMatchesErrors.InvalidPageNumber);
        if (query.PageSize is <= 0 or > 100)
            return Result<MatchGoalsPage>.Failure(GetMatchesErrors.InvalidPageSize);
        if (query.TeamId == Guid.Empty)
            return Result<MatchGoalsPage>.Failure(GetMatchesErrors.InvalidTeamId);
        var sortField = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "minute" => GoalSortField.Minute,
            "playername" => GoalSortField.PlayerName,
            "teamname" => GoalSortField.TeamName,
            "id" => GoalSortField.Id,
            _ => (GoalSortField)(-1)
        };
        if (!Enum.IsDefined(sortField))
            return Result<MatchGoalsPage>.Failure(new Error("Goals.InvalidSortBy",
                "SortBy must be minute, playerName, teamName or id.", ErrorType.Validation));
        var direction = query.SortDirection?.Trim().ToLowerInvariant() switch
        {
            "asc" => MatchSortDirection.Ascending,
            "desc" => MatchSortDirection.Descending,
            _ => (MatchSortDirection)(-1)
        };
        if (!Enum.IsDefined(direction))
            return Result<MatchGoalsPage>.Failure(GetMatchesErrors.InvalidSortDirection);
        var match = await matches.FindByIdAsync(query.MatchId, cancellationToken);
        if (match is null)
            return Result<MatchGoalsPage>.Failure(MatchMutationErrors.NotFound);
        return Result<MatchGoalsPage>.Success(await matches.GetGoalsAsync(new(
            query.MatchId, match.HomeTeamId, match.AwayTeamId, query.TeamId,
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            query.PageNumber, query.PageSize, sortField, direction), cancellationToken));
    }
}
