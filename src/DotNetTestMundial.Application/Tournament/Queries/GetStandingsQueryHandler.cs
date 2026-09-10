// Responsabilidad del archivo: Valida los parámetros de la tabla de posiciones y solicita su página calculada.
// Relación en el sistema: Convierte texto HTTP en enums seguros y delega todo el cálculo agregado al puerto Dapper.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Tournament.Queries;

public sealed class GetStandingsQueryHandler(ITournamentReadRepository tournament)
    : IQueryHandler<GetStandingsQuery, PagedResult<StandingListItem>>
{
    public async Task<Result<PagedResult<StandingListItem>>> HandleAsync(
        GetStandingsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageNumber < 1)
            return Result<PagedResult<StandingListItem>>.Failure(TournamentQueryErrors.InvalidPageNumber);
        if (query.PageSize is < 1 or > 100)
            return Result<PagedResult<StandingListItem>>.Failure(TournamentQueryErrors.InvalidPageSize);
        if (!TrySortField(query.SortBy, out var sortField))
            return Result<PagedResult<StandingListItem>>.Failure(TournamentQueryErrors.InvalidStandingSortBy);
        if (!TryDirection(query.SortDirection, out var direction))
            return Result<PagedResult<StandingListItem>>.Failure(TournamentQueryErrors.InvalidSortDirection);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var specification = new StandingPageSpecification(
            search, query.PageNumber, query.PageSize, sortField, direction);
        return Result<PagedResult<StandingListItem>>.Success(
            await tournament.GetStandingsAsync(specification, cancellationToken));
    }

    private static bool TrySortField(string? value, out StandingSortField field)
    {
        field = value?.Trim().ToLowerInvariant() switch
        {
            "points" => StandingSortField.Points,
            "goaldifference" => StandingSortField.GoalDifference,
            "goalsfor" => StandingSortField.GoalsFor,
            "played" => StandingSortField.Played,
            "won" => StandingSortField.Won,
            "teamname" => StandingSortField.TeamName,
            _ => (StandingSortField)(-1)
        };
        return Enum.IsDefined(field);
    }

    internal static bool TryDirection(string? value, out TournamentSortDirection direction)
    {
        direction = value?.Trim().ToLowerInvariant() switch
        {
            "asc" => TournamentSortDirection.Ascending,
            "desc" => TournamentSortDirection.Descending,
            _ => (TournamentSortDirection)(-1)
        };
        return Enum.IsDefined(direction);
    }
}
