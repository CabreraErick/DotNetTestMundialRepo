// Responsabilidad del archivo: Ejecuta con Dapper el calendario, detalle, goles y estado completo de partidos.
// Relación en el sistema: Implementa IMatchReadRepository y compone proyecciones relacionadas sin usar EF Core.
using Dapper;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Results;
using Microsoft.Data.SqlClient;

namespace DotNetTestMundial.Infrastructure.Persistence.Queries;

internal sealed class SqlMatchReadRepository(string connectionString) : IMatchReadRepository
{
    public async Task<MatchListItem?> FindByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT m.Id,
                   m.HomeTeamId,
                   home.Name AS HomeTeamName,
                   m.AwayTeamId,
                   away.Name AS AwayTeamName,
                   m.ScheduledAt,
                   m.Status,
                   m.HomeScore,
                   m.AwayScore,
                   (SELECT COUNT_BIG(1) FROM dbo.Goals g WHERE g.MatchId = m.Id) AS GoalCount
            FROM dbo.Matches m
            INNER JOIN dbo.Teams home ON home.Id = m.HomeTeamId
            INNER JOIN dbo.Teams away ON away.Id = m.AwayTeamId
            WHERE m.Id = @id;
            """;
        await using var connection = new SqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<MatchListItem>(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> HasTeamScheduleConflictAsync(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt,
        Guid? excludingMatchId = null,
        CancellationToken cancellationToken = default)
    {
        var dayStart = scheduledAt.Date;
        var dayEnd = dayStart.AddDays(1);
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1
                FROM dbo.Matches
                WHERE ScheduledAt >= @dayStart
                  AND ScheduledAt < @dayEnd
                  AND Status <> 3
                  AND (@excludingMatchId IS NULL OR Id <> @excludingMatchId)
                  AND (HomeTeamId IN (@homeTeamId, @awayTeamId)
                       OR AwayTeamId IN (@homeTeamId, @awayTeamId)))
            THEN 1 ELSE 0 END AS bit);
            """;
        await using var connection = new SqlConnection(connectionString);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new
        {
            homeTeamId,
            awayTeamId,
            dayStart,
            dayEnd,
            excludingMatchId
        }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<MatchListItem>> GetPageAsync(
        MatchPageSpecification specification,
        CancellationToken cancellationToken = default)
    {
        // Identifiers come only from validated enums; filters remain SQL parameters.
        var orderColumn = specification.SortField switch
        {
            MatchSortField.Id => "m.Id",
            MatchSortField.HomeTeamId => "m.HomeTeamId",
            MatchSortField.AwayTeamId => "m.AwayTeamId",
            MatchSortField.ScheduledAt => "m.ScheduledAt",
            MatchSortField.Status => "m.Status",
            _ => throw new ArgumentOutOfRangeException(nameof(specification))
        };
        var orderDirection = specification.SortDirection == MatchSortDirection.Ascending
            ? "ASC"
            : "DESC";
        var tieBreaker = specification.SortField == MatchSortField.Id ? string.Empty : ", m.Id ASC";
        var sql = $$"""
            SELECT COUNT_BIG(1)
            FROM dbo.Matches m
            WHERE (@TeamId IS NULL OR m.HomeTeamId = @TeamId OR m.AwayTeamId = @TeamId)
              AND (@Status IS NULL OR m.Status = @Status)
              AND (@From IS NULL OR m.ScheduledAt >= @From)
              AND (@To IS NULL OR m.ScheduledAt <= @To);

            SELECT m.Id,
                   m.HomeTeamId,
                   home.Name AS HomeTeamName,
                   m.AwayTeamId,
                   away.Name AS AwayTeamName,
                   m.ScheduledAt,
                   m.Status,
                   m.HomeScore,
                   m.AwayScore,
                   (SELECT COUNT_BIG(1) FROM dbo.Goals g WHERE g.MatchId = m.Id) AS GoalCount
            FROM dbo.Matches m
            INNER JOIN dbo.Teams home ON home.Id = m.HomeTeamId
            INNER JOIN dbo.Teams away ON away.Id = m.AwayTeamId
            WHERE (@TeamId IS NULL OR m.HomeTeamId = @TeamId OR m.AwayTeamId = @TeamId)
              AND (@Status IS NULL OR m.Status = @Status)
              AND (@From IS NULL OR m.ScheduledAt >= @From)
              AND (@To IS NULL OR m.ScheduledAt <= @To)
            ORDER BY {{orderColumn}} {{orderDirection}}{{tieBreaker}}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        var offset = ((long)specification.PageNumber - 1) * specification.PageSize;
        var command = new CommandDefinition(sql, new
        {
            specification.TeamId,
            Status = specification.Status is null ? null : (int?)specification.Status,
            specification.From,
            specification.To,
            Offset = offset,
            specification.PageSize
        }, cancellationToken: cancellationToken);

        await using var connection = new SqlConnection(connectionString);
        using var grid = await connection.QueryMultipleAsync(command);
        var totalRecords = await grid.ReadSingleAsync<long>();
        var data = (await grid.ReadAsync<MatchListItem>()).AsList();
        return PagedResult<MatchListItem>.Create(
            data, specification.PageNumber, specification.PageSize, totalRecords);
    }

    public async Task<IReadOnlyList<GoalListItem>> GetGoalsAsync(
        Guid matchId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT g.Id,
                   g.MatchId,
                   g.PlayerId,
                   p.Name AS PlayerName,
                   g.TeamId,
                   t.Name AS TeamName,
                   g.Minute
            FROM dbo.Goals g
            INNER JOIN dbo.Players p ON p.Id = g.PlayerId
            INNER JOIN dbo.Teams t ON t.Id = g.TeamId
            WHERE g.MatchId = @matchId
            ORDER BY g.Minute ASC, g.Id ASC;
            """;
        await using var connection = new SqlConnection(connectionString);
        return (await connection.QueryAsync<GoalListItem>(new CommandDefinition(
            sql, new { matchId }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<MatchStateSnapshot?> FindStateByIdAsync(
        Guid matchId, CancellationToken cancellationToken = default)
    {
        // Both result sets share one command and connection, so Application receives
        // the match and the exact goal collection used to validate its final score.
        const string sql = """
            SELECT m.Id,
                   m.HomeTeamId,
                   home.Name AS HomeTeamName,
                   m.AwayTeamId,
                   away.Name AS AwayTeamName,
                   m.ScheduledAt,
                   m.Status,
                   m.HomeScore,
                   m.AwayScore,
                   (SELECT COUNT_BIG(1) FROM dbo.Goals g WHERE g.MatchId = m.Id) AS GoalCount
            FROM dbo.Matches m
            INNER JOIN dbo.Teams home ON home.Id = m.HomeTeamId
            INNER JOIN dbo.Teams away ON away.Id = m.AwayTeamId
            WHERE m.Id = @matchId;

            SELECT g.Id,
                   g.MatchId,
                   g.PlayerId,
                   p.Name AS PlayerName,
                   g.TeamId,
                   t.Name AS TeamName,
                   g.Minute
            FROM dbo.Goals g
            INNER JOIN dbo.Players p ON p.Id = g.PlayerId
            INNER JOIN dbo.Teams t ON t.Id = g.TeamId
            WHERE g.MatchId = @matchId
            ORDER BY g.Minute ASC, g.Id ASC;
            """;
        await using var connection = new SqlConnection(connectionString);
        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, new { matchId }, cancellationToken: cancellationToken));
        var match = await grid.ReadSingleOrDefaultAsync<MatchListItem>();
        if (match is null)
            return null;
        var goals = (await grid.ReadAsync<GoalListItem>()).AsList();
        return new MatchStateSnapshot(match, goals);
    }
}
