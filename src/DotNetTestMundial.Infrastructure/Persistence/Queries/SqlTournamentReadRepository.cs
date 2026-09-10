// Responsabilidad del archivo: Calcula con Dapper las páginas de posiciones y goleadores directamente en SQL Server.
// Relación en el sistema: Implementa ITournamentReadRepository y devuelve DTOs agregados sin materializar ni rastrear entidades EF.
using Dapper;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Tournament.Queries;
using DotNetTestMundial.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace DotNetTestMundial.Infrastructure.Persistence.Queries;

internal sealed class SqlTournamentReadRepository(string connectionString)
    : ITournamentReadRepository
{
    public async Task<PagedResult<StandingListItem>> GetStandingsAsync(
        StandingPageSpecification specification,
        CancellationToken cancellationToken = default)
    {
        var direction = specification.SortDirection == TournamentSortDirection.Ascending
            ? "ASC"
            : "DESC";
        // Every fragment comes from enums produced in Application; client text is never
        // concatenated into SQL. Additional columns make page order deterministic.
        var orderClause = specification.SortField switch
        {
            StandingSortField.Points =>
                $"Points {direction}, GoalDifference DESC, GoalsFor DESC, TeamName ASC, TeamId ASC",
            StandingSortField.GoalDifference =>
                $"GoalDifference {direction}, Points DESC, GoalsFor DESC, TeamName ASC, TeamId ASC",
            StandingSortField.GoalsFor =>
                $"GoalsFor {direction}, Points DESC, GoalDifference DESC, TeamName ASC, TeamId ASC",
            StandingSortField.Played =>
                $"Played {direction}, Points DESC, GoalDifference DESC, TeamName ASC, TeamId ASC",
            StandingSortField.Won =>
                $"Won {direction}, Points DESC, GoalDifference DESC, TeamName ASC, TeamId ASC",
            StandingSortField.TeamName => $"TeamName {direction}, TeamId ASC",
            _ => throw new ArgumentOutOfRangeException(nameof(specification))
        };

        var sql = $$"""
            WITH MatchSides AS
            (
                SELECT HomeTeamId AS TeamId, HomeScore AS GoalsFor, AwayScore AS GoalsAgainst
                FROM dbo.Matches WHERE Status = @PlayedStatus
                UNION ALL
                SELECT AwayTeamId, AwayScore, HomeScore
                FROM dbo.Matches WHERE Status = @PlayedStatus
            ),
            TeamStats AS
            (
                SELECT t.Id AS TeamId,
                       t.Name AS TeamName,
                       t.ShortName,
                       COUNT_BIG(s.TeamId) AS Played,
                       COALESCE(SUM(CONVERT(bigint, CASE WHEN s.GoalsFor > s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Won,
                       COALESCE(SUM(CONVERT(bigint, CASE WHEN s.GoalsFor = s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Drawn,
                       COALESCE(SUM(CONVERT(bigint, CASE WHEN s.GoalsFor < s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Lost,
                       COALESCE(SUM(CONVERT(bigint, s.GoalsFor)), 0) AS GoalsFor,
                       COALESCE(SUM(CONVERT(bigint, s.GoalsAgainst)), 0) AS GoalsAgainst,
                       COALESCE(SUM(CONVERT(bigint, s.GoalsFor - s.GoalsAgainst)), 0) AS GoalDifference,
                       COALESCE(SUM(CONVERT(bigint,
                           CASE WHEN s.GoalsFor > s.GoalsAgainst THEN 3
                                WHEN s.GoalsFor = s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Points
                FROM dbo.Teams t
                LEFT JOIN MatchSides s ON s.TeamId = t.Id
                WHERE @Pattern IS NULL OR t.Name LIKE @Pattern OR t.ShortName LIKE @Pattern
                GROUP BY t.Id, t.Name, t.ShortName
            )
            SELECT COUNT_BIG(1) FROM TeamStats;

            WITH MatchSides AS
            (
                SELECT HomeTeamId AS TeamId, HomeScore AS GoalsFor, AwayScore AS GoalsAgainst
                FROM dbo.Matches WHERE Status = @PlayedStatus
                UNION ALL
                SELECT AwayTeamId, AwayScore, HomeScore
                FROM dbo.Matches WHERE Status = @PlayedStatus
            ),
            TeamStats AS
            (
                SELECT t.Id AS TeamId,
                       t.Name AS TeamName,
                       t.ShortName,
                       COUNT_BIG(s.TeamId) AS Played,
                       COALESCE(SUM(CONVERT(bigint, CASE WHEN s.GoalsFor > s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Won,
                       COALESCE(SUM(CONVERT(bigint, CASE WHEN s.GoalsFor = s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Drawn,
                       COALESCE(SUM(CONVERT(bigint, CASE WHEN s.GoalsFor < s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Lost,
                       COALESCE(SUM(CONVERT(bigint, s.GoalsFor)), 0) AS GoalsFor,
                       COALESCE(SUM(CONVERT(bigint, s.GoalsAgainst)), 0) AS GoalsAgainst,
                       COALESCE(SUM(CONVERT(bigint, s.GoalsFor - s.GoalsAgainst)), 0) AS GoalDifference,
                       COALESCE(SUM(CONVERT(bigint,
                           CASE WHEN s.GoalsFor > s.GoalsAgainst THEN 3
                                WHEN s.GoalsFor = s.GoalsAgainst THEN 1 ELSE 0 END)), 0) AS Points
                FROM dbo.Teams t
                LEFT JOIN MatchSides s ON s.TeamId = t.Id
                WHERE @Pattern IS NULL OR t.Name LIKE @Pattern OR t.ShortName LIKE @Pattern
                GROUP BY t.Id, t.Name, t.ShortName
            ),
            Ranked AS
            (
                SELECT ROW_NUMBER() OVER (
                    ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC,
                             TeamName ASC, TeamId ASC) AS Position, *
                FROM TeamStats
            )
            SELECT Position, TeamId, TeamName, ShortName, Played, Won, Drawn, Lost,
                   GoalsFor, GoalsAgainst, GoalDifference, Points
            FROM Ranked
            ORDER BY {{orderClause}}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var pattern = specification.Search is null
            ? null
            : CreateLiteralLikePattern(specification.Search);
        var offset = ((long)specification.PageNumber - 1) * specification.PageSize;
        await using var connection = new SqlConnection(connectionString);
        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, new
            {
                PlayedStatus = (int)MatchStatus.Played,
                Pattern = pattern,
                Offset = offset,
                specification.PageSize
            },
            cancellationToken: cancellationToken));
        var totalRecords = await grid.ReadSingleAsync<long>();
        var data = (await grid.ReadAsync<StandingListItem>()).AsList();
        return PagedResult<StandingListItem>.Create(
            data, specification.PageNumber, specification.PageSize, totalRecords);
    }

    public async Task<PagedResult<ScorerListItem>> GetScorersAsync(
        ScorerPageSpecification specification,
        CancellationToken cancellationToken = default)
    {
        var direction = specification.SortDirection == TournamentSortDirection.Ascending
            ? "ASC"
            : "DESC";
        var orderClause = specification.SortField switch
        {
            ScorerSortField.Goals => $"Goals {direction}, PlayerName ASC, PlayerId ASC",
            ScorerSortField.PlayerName => $"PlayerName {direction}, PlayerId ASC",
            ScorerSortField.TeamName => $"TeamName {direction}, PlayerName ASC, PlayerId ASC",
            _ => throw new ArgumentOutOfRangeException(nameof(specification))
        };

        var sql = $$"""
            WITH ScorerStats AS
            (
                SELECT p.Id AS PlayerId,
                       p.Name AS PlayerName,
                       p.TeamId,
                       t.Name AS TeamName,
                       COUNT_BIG(1) AS Goals
                FROM dbo.Goals g
                INNER JOIN dbo.Matches m ON m.Id = g.MatchId AND m.Status = @PlayedStatus
                INNER JOIN dbo.Players p ON p.Id = g.PlayerId AND p.TeamId = g.TeamId
                INNER JOIN dbo.Teams t ON t.Id = p.TeamId
                WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
                  AND (@Pattern IS NULL OR p.Name LIKE @Pattern OR t.Name LIKE @Pattern)
                GROUP BY p.Id, p.Name, p.TeamId, t.Name
            )
            SELECT COUNT_BIG(1) FROM ScorerStats;

            WITH ScorerStats AS
            (
                SELECT p.Id AS PlayerId,
                       p.Name AS PlayerName,
                       p.TeamId,
                       t.Name AS TeamName,
                       COUNT_BIG(1) AS Goals
                FROM dbo.Goals g
                INNER JOIN dbo.Matches m ON m.Id = g.MatchId AND m.Status = @PlayedStatus
                INNER JOIN dbo.Players p ON p.Id = g.PlayerId AND p.TeamId = g.TeamId
                INNER JOIN dbo.Teams t ON t.Id = p.TeamId
                WHERE (@TeamId IS NULL OR p.TeamId = @TeamId)
                  AND (@Pattern IS NULL OR p.Name LIKE @Pattern OR t.Name LIKE @Pattern)
                GROUP BY p.Id, p.Name, p.TeamId, t.Name
            ),
            Ranked AS
            (
                SELECT ROW_NUMBER() OVER (
                    ORDER BY Goals DESC, PlayerName ASC, PlayerId ASC) AS Position, *
                FROM ScorerStats
            )
            SELECT Position, PlayerId, PlayerName, TeamId, TeamName, Goals
            FROM Ranked
            ORDER BY {{orderClause}}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var pattern = specification.Search is null
            ? null
            : CreateLiteralLikePattern(specification.Search);
        var offset = ((long)specification.PageNumber - 1) * specification.PageSize;
        await using var connection = new SqlConnection(connectionString);
        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(sql, new
        {
            specification.TeamId,
            PlayedStatus = (int)MatchStatus.Played,
            Pattern = pattern,
            Offset = offset,
            specification.PageSize
        }, cancellationToken: cancellationToken));
        var totalRecords = await grid.ReadSingleAsync<long>();
        var data = (await grid.ReadAsync<ScorerListItem>()).AsList();
        return PagedResult<ScorerListItem>.Create(
            data, specification.PageNumber, specification.PageSize, totalRecords);
    }

    private static string CreateLiteralLikePattern(string search) =>
        $"%{search.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal)}%";
}
