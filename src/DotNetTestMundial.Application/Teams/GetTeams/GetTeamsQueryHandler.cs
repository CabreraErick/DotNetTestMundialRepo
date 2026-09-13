// Responsabilidad del archivo: Valida y normaliza la solicitud de listado de equipos.
// Relación en el sistema: Sólo llama ITeamReadRepository y nunca utiliza EF Core ni Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Teams.GetTeams;

/// <summary>
/// Application orchestrator for the team list. It converts untrusted HTTP values into
/// a safe specification and delegates the database work to the read-side repository.
/// </summary>
public sealed class GetTeamsQueryHandler(ITeamReadRepository teams)
    : IQueryHandler<GetTeamsQuery, PagedResult<TeamListItem>>
{
    public async Task<Result<PagedResult<TeamListItem>>> HandleAsync(
        GetTeamsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageNumber < 1)
            return Result<PagedResult<TeamListItem>>.Failure(GetTeamsErrors.InvalidPageNumber);
        if (query.PageSize is < 1 or > 100)
            return Result<PagedResult<TeamListItem>>.Failure(GetTeamsErrors.InvalidPageSize);
        if (!TrySortField(query.SortBy, out var sortField))
            return Result<PagedResult<TeamListItem>>.Failure(GetTeamsErrors.InvalidSortBy);
        if (!TryDirection(query.SortDirection, out var direction))
            return Result<PagedResult<TeamListItem>>.Failure(GetTeamsErrors.InvalidSortDirection);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var specification = new TeamPageSpecification(
            search, query.PageNumber, query.PageSize, sortField, direction);
        return Result<PagedResult<TeamListItem>>.Success(
            await teams.GetPageAsync(specification, cancellationToken));
    }

    private static bool TrySortField(string? value, out TeamSortField field)
    {
        field = value?.Trim().ToLowerInvariant() switch
        {
            "id" => TeamSortField.Id,
            "name" => TeamSortField.Name,
            "shortname" => TeamSortField.ShortName,
            _ => (TeamSortField)(-1)
        };
        return Enum.IsDefined(field);
    }

    private static bool TryDirection(string? value, out QuerySortDirection direction)
    {
        direction = value?.Trim().ToLowerInvariant() switch
        {
            "asc" => QuerySortDirection.Ascending,
            "desc" => QuerySortDirection.Descending,
            _ => (QuerySortDirection)(-1)
        };
        return Enum.IsDefined(direction);
    }
}
