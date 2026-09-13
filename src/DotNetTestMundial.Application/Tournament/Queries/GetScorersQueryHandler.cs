// Responsabilidad del archivo: Valida filtros y orden de la clasificación de goleadores.
// Relación en el sistema: Produce una especificación segura para ITournamentReadRepository y nunca usa EF Core.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Tournament.Queries;

public sealed class GetScorersQueryHandler(ITournamentReadRepository tournament)
    : IQueryHandler<GetScorersQuery, PagedResult<ScorerListItem>>
{
    public async Task<Result<PagedResult<ScorerListItem>>> HandleAsync(
        GetScorersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageNumber < 1)
            return Result<PagedResult<ScorerListItem>>.Failure(TournamentQueryErrors.InvalidPageNumber);
        if (query.PageSize is < 1 or > 100)
            return Result<PagedResult<ScorerListItem>>.Failure(TournamentQueryErrors.InvalidPageSize);
        if (!TrySortField(query.SortBy, out var sortField))
            return Result<PagedResult<ScorerListItem>>.Failure(TournamentQueryErrors.InvalidScorerSortBy);
        if (!GetStandingsQueryHandler.TryDirection(query.SortDirection, out var direction))
            return Result<PagedResult<ScorerListItem>>.Failure(TournamentQueryErrors.InvalidSortDirection);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var specification = new ScorerPageSpecification(
            search, query.TeamId, query.PageNumber, query.PageSize, sortField, direction);
        return Result<PagedResult<ScorerListItem>>.Success(
            await tournament.GetScorersAsync(specification, cancellationToken));
    }

    private static bool TrySortField(string? value, out ScorerSortField field)
    {
        field = value?.Trim().ToLowerInvariant() switch
        {
            "goals" => ScorerSortField.Goals,
            "playername" => ScorerSortField.PlayerName,
            "teamname" => ScorerSortField.TeamName,
            _ => (ScorerSortField)(-1)
        };
        return Enum.IsDefined(field);
    }
}
