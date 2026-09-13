// Responsabilidad del archivo: Valida y normaliza la consulta paginada de jugadores.
// Relación en el sistema: Convierte texto HTTP en una especificación segura y delega la lectura al puerto Dapper.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Players.GetPlayers;

public sealed class GetPlayersQueryHandler(IPlayerReadRepository players)
    : IQueryHandler<GetPlayersQuery, PagedResult<PlayerListItem>>
{
    public async Task<Result<PagedResult<PlayerListItem>>> HandleAsync(
        GetPlayersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TeamId == Guid.Empty)
            return Result<PagedResult<PlayerListItem>>.Failure(GetPlayersErrors.InvalidTeamId);
        if (query.PageNumber <= 0)
            return Result<PagedResult<PlayerListItem>>.Failure(GetPlayersErrors.InvalidPageNumber);
        if (query.PageSize is <= 0 or > 100)
            return Result<PagedResult<PlayerListItem>>.Failure(GetPlayersErrors.InvalidPageSize);
        if (!TrySortField(query.SortBy, out var sortField))
            return Result<PagedResult<PlayerListItem>>.Failure(GetPlayersErrors.InvalidSortBy);
        if (!TryDirection(query.SortDirection, out var direction))
            return Result<PagedResult<PlayerListItem>>.Failure(GetPlayersErrors.InvalidSortDirection);

        var specification = new PlayerPageSpecification(
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            query.TeamId,
            query.IsActive,
            query.PageNumber,
            query.PageSize,
            sortField,
            direction);
        var page = await players.GetPageAsync(specification, cancellationToken);
        return Result<PagedResult<PlayerListItem>>.Success(page);
    }

    private static bool TrySortField(string? value, out PlayerSortField field)
    {
        field = value?.Trim().ToLowerInvariant() switch
        {
            "id" => PlayerSortField.Id,
            "teamid" => PlayerSortField.TeamId,
            "name" => PlayerSortField.Name,
            "jerseynumber" => PlayerSortField.JerseyNumber,
            "isactive" => PlayerSortField.IsActive,
            _ => (PlayerSortField)(-1)
        };
        return Enum.IsDefined(field);
    }

    private static bool TryDirection(string? value, out PlayerSortDirection direction)
    {
        direction = value?.Trim().ToLowerInvariant() switch
        {
            "asc" => PlayerSortDirection.Ascending,
            "desc" => PlayerSortDirection.Descending,
            _ => (PlayerSortDirection)(-1)
        };
        return Enum.IsDefined(direction);
    }
}
