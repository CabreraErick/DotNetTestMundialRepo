// Responsabilidad del archivo: Valida y normaliza filtros paginados de partidos.
// Relación en el sistema: Produce enums seguros para el ORDER BY y delega la consulta al repositorio Dapper.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Matches.GetMatches;

public sealed class GetMatchesQueryHandler(IMatchReadRepository matches)
    : IQueryHandler<GetMatchesQuery, PagedResult<MatchListItem>>
{
    public async Task<Result<PagedResult<MatchListItem>>> HandleAsync(
        GetMatchesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TeamId == Guid.Empty)
            return Result<PagedResult<MatchListItem>>.Failure(GetMatchesErrors.InvalidTeamId);
        if (!TryStatus(query.Status, out var status))
            return Result<PagedResult<MatchListItem>>.Failure(GetMatchesErrors.InvalidStatus);
        if (query.From > query.To)
            return Result<PagedResult<MatchListItem>>.Failure(GetMatchesErrors.InvalidDateRange);
        if (query.PageNumber <= 0)
            return Result<PagedResult<MatchListItem>>.Failure(GetMatchesErrors.InvalidPageNumber);
        if (query.PageSize is <= 0 or > 100)
            return Result<PagedResult<MatchListItem>>.Failure(GetMatchesErrors.InvalidPageSize);
        if (!TrySortField(query.SortBy, out var sortField))
            return Result<PagedResult<MatchListItem>>.Failure(GetMatchesErrors.InvalidSortBy);
        if (!TryDirection(query.SortDirection, out var direction))
            return Result<PagedResult<MatchListItem>>.Failure(GetMatchesErrors.InvalidSortDirection);

        var page = await matches.GetPageAsync(new(
            query.TeamId,
            status,
            query.From,
            query.To,
            query.PageNumber,
            query.PageSize,
            sortField,
            direction), cancellationToken);
        return Result<PagedResult<MatchListItem>>.Success(page);
    }

    private static bool TryStatus(string? value, out MatchStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (!Enum.TryParse<MatchStatus>(value.Trim(), true, out var parsed) || !Enum.IsDefined(parsed))
            return false;
        status = parsed;
        return true;
    }

    private static bool TrySortField(string? value, out MatchSortField field)
    {
        field = value?.Trim().ToLowerInvariant() switch
        {
            "id" => MatchSortField.Id,
            "hometeamid" => MatchSortField.HomeTeamId,
            "awayteamid" => MatchSortField.AwayTeamId,
            "scheduledat" => MatchSortField.ScheduledAt,
            "status" => MatchSortField.Status,
            _ => (MatchSortField)(-1)
        };
        return Enum.IsDefined(field);
    }

    private static bool TryDirection(string? value, out MatchSortDirection direction)
    {
        direction = value?.Trim().ToLowerInvariant() switch
        {
            "asc" => MatchSortDirection.Ascending,
            "desc" => MatchSortDirection.Descending,
            _ => (MatchSortDirection)(-1)
        };
        return Enum.IsDefined(direction);
    }
}
