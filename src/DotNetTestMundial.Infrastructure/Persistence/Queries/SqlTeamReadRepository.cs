// Responsabilidad del archivo: Ejecuta el listado paginado de equipos con Dapper.
// Relación en el sistema: Implementa ITeamReadRepository y devuelve DTOs sin seguimiento de EF.
using Dapper;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Teams.GetTeams;
using Microsoft.Data.SqlClient;

namespace DotNetTestMundial.Infrastructure.Persistence.Queries;

/// <summary>
/// Dapper implementation of the team read port. It returns DTO projections directly,
/// keeping EF Core and tracked Domain entities out of the Query path.
/// </summary>
internal sealed class SqlTeamReadRepository(string connectionString) : ITeamReadRepository
{
    public async Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, Name, ShortName FROM dbo.Teams WHERE Id = @id;";
        await using var connection = new SqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<TeamListItem>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<TeamListItem>> GetPageAsync(
        TeamPageSpecification specification,
        CancellationToken cancellationToken = default)
    {
        // ORDER BY identifiers cannot be SQL parameters. Both fragments come exclusively
        // from validated enums, never from the original query-string text.
        var orderColumn = specification.SortField switch
        {
            TeamSortField.Id => "Id",
            TeamSortField.Name => "Name",
            TeamSortField.ShortName => "ShortName",
            _ => throw new ArgumentOutOfRangeException(nameof(specification))
        };
        var orderDirection = specification.SortDirection == QuerySortDirection.Ascending
            ? "ASC"
            : "DESC";
        var deterministicTieBreaker = specification.SortField == TeamSortField.Id ? string.Empty : ", Id ASC";

        var sql = $$"""
            SELECT COUNT_BIG(1)
            FROM dbo.Teams
            WHERE @Pattern IS NULL OR Name LIKE @Pattern OR ShortName LIKE @Pattern;

            SELECT Id, Name, ShortName
            FROM dbo.Teams
            WHERE @Pattern IS NULL OR Name LIKE @Pattern OR ShortName LIKE @Pattern
            ORDER BY {{orderColumn}} {{orderDirection}}{{deterministicTieBreaker}}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var pattern = specification.Search is null ? null : CreateLiteralLikePattern(specification.Search);
        var offset = ((long)specification.PageNumber - 1) * specification.PageSize;
        var command = new CommandDefinition(sql, new { Pattern = pattern, Offset = offset, specification.PageSize },
            cancellationToken: cancellationToken);

        await using var connection = new SqlConnection(connectionString);
        using var grid = await connection.QueryMultipleAsync(command);
        var totalRecords = await grid.ReadSingleAsync<long>();
        var data = (await grid.ReadAsync<TeamListItem>()).AsList();
        return PagedResult<TeamListItem>.Create(data, specification.PageNumber,
            specification.PageSize, totalRecords);
    }

    private static string CreateLiteralLikePattern(string search) =>
        $"%{search.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal)}%";
}
