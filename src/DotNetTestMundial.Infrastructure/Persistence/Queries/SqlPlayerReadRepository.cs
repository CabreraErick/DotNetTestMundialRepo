// Responsabilidad del archivo: Ejecuta con Dapper el detalle y listado paginado de jugadores.
// Relación en el sistema: Implementa IPlayerReadRepository sin seguimiento EF y entrega proyecciones a Application.
using Dapper;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Players.GetPlayers;
using Microsoft.Data.SqlClient;

namespace DotNetTestMundial.Infrastructure.Persistence.Queries;

internal sealed class SqlPlayerReadRepository(string connectionString) : IPlayerReadRepository
{
    public async Task<PlayerListItem?> FindByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, TeamId, Name, JerseyNumber, IsActive
            FROM dbo.Players
            WHERE Id = @id;
            """;
        await using var connection = new SqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<PlayerListItem>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> IsJerseyNumberInUseAsync(
        Guid teamId,
        int jerseyNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1
                FROM dbo.Players
                WHERE TeamId = @teamId
                  AND JerseyNumber = @jerseyNumber
                  AND (@excludingId IS NULL OR Id <> @excludingId))
            THEN 1 ELSE 0 END AS bit);
            """;
        await using var connection = new SqlConnection(connectionString);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { teamId, jerseyNumber, excludingId }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<PlayerListItem>> GetPageAsync(
        PlayerPageSpecification specification,
        CancellationToken cancellationToken = default)
    {
        // Only validated enums select identifiers; all user values remain SQL parameters.
        var orderColumn = specification.SortField switch
        {
            PlayerSortField.Id => "Id",
            PlayerSortField.TeamId => "TeamId",
            PlayerSortField.Name => "Name",
            PlayerSortField.JerseyNumber => "JerseyNumber",
            PlayerSortField.IsActive => "IsActive",
            _ => throw new ArgumentOutOfRangeException(nameof(specification))
        };
        var orderDirection = specification.SortDirection == PlayerSortDirection.Ascending
            ? "ASC"
            : "DESC";
        var tieBreaker = specification.SortField == PlayerSortField.Id ? string.Empty : ", Id ASC";
        var sql = $$"""
            SELECT COUNT_BIG(1)
            FROM dbo.Players
            WHERE (@TeamId IS NULL OR TeamId = @TeamId)
              AND (@IsActive IS NULL OR IsActive = @IsActive)
              AND (@Pattern IS NULL OR Name LIKE @Pattern);

            SELECT Id, TeamId, Name, JerseyNumber, IsActive
            FROM dbo.Players
            WHERE (@TeamId IS NULL OR TeamId = @TeamId)
              AND (@IsActive IS NULL OR IsActive = @IsActive)
              AND (@Pattern IS NULL OR Name LIKE @Pattern)
            ORDER BY {{orderColumn}} {{orderDirection}}{{tieBreaker}}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        var pattern = specification.Search is null
            ? null
            : CreateLiteralLikePattern(specification.Search);
        var offset = ((long)specification.PageNumber - 1) * specification.PageSize;
        var command = new CommandDefinition(sql, new
        {
            specification.TeamId,
            specification.IsActive,
            Pattern = pattern,
            Offset = offset,
            specification.PageSize
        }, cancellationToken: cancellationToken);

        await using var connection = new SqlConnection(connectionString);
        using var grid = await connection.QueryMultipleAsync(command);
        var totalRecords = await grid.ReadSingleAsync<long>();
        var data = (await grid.ReadAsync<PlayerListItem>()).AsList();
        return PagedResult<PlayerListItem>.Create(
            data, specification.PageNumber, specification.PageSize, totalRecords);
    }

    private static string CreateLiteralLikePattern(string search) =>
        $"%{search.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal)}%";
}
