using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// Responsabilidad del archivo: corrige datos QA redundantes y aplica la exclusividad diaria de equipos.
// Relación en el sistema: sincroniza la protección concurrente de SQL Server con la validación preventiva de Application.

namespace DotNetTestMundial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreventSameDayMatchesAndSupportTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS dbo.TR_Matches_RejectTeamScheduleConflict;

                DELETE redundant
                FROM dbo.Matches AS redundant
                INNER JOIN dbo.Teams AS home ON home.Id = redundant.HomeTeamId
                INNER JOIN dbo.Teams AS away ON away.Id = redundant.AwayTeamId
                WHERE home.Name = N'Colombia'
                  AND away.Name = N'España'
                  AND redundant.ScheduledAt = '2026-09-14T16:52:00'
                  AND redundant.Status = 1
                  AND NOT EXISTS (SELECT 1 FROM dbo.Goals AS goal WHERE goal.MatchId = redundant.Id)
                  AND EXISTS (
                      SELECT 1
                      FROM dbo.Matches AS retained
                      INNER JOIN dbo.Teams AS retainedHome ON retainedHome.Id = retained.HomeTeamId
                      INNER JOIN dbo.Teams AS retainedAway ON retainedAway.Id = retained.AwayTeamId
                      WHERE retained.ScheduledAt = '2026-09-14T16:51:00'
                        AND retainedHome.Name = N'España'
                        AND retainedAway.Name = N'Egipto'
                        AND EXISTS (SELECT 1 FROM dbo.Goals AS goal WHERE goal.MatchId = retained.Id));
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER dbo.TR_Matches_RejectTeamScheduleConflict
                ON dbo.Matches
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS candidate
                        INNER JOIN dbo.Matches AS existing WITH (UPDLOCK, HOLDLOCK)
                            ON existing.Id <> candidate.Id
                           AND existing.Status <> 3
                           AND existing.ScheduledAt >= CONVERT(date, candidate.ScheduledAt)
                           AND existing.ScheduledAt < DATEADD(day, 1, CONVERT(date, candidate.ScheduledAt))
                        WHERE candidate.Status <> 3
                          AND (candidate.HomeTeamId IN (existing.HomeTeamId, existing.AwayTeamId)
                               OR candidate.AwayTeamId IN (existing.HomeTeamId, existing.AwayTeamId))
                    )
                    BEGIN
                        THROW 51001, 'Matches.ScheduleConflict', 1;
                    END
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS dbo.TR_Matches_RejectTeamScheduleConflict;");

            migrationBuilder.Sql(
                """
                CREATE TRIGGER dbo.TR_Matches_RejectTeamScheduleConflict
                ON dbo.Matches
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted AS candidate
                        INNER JOIN dbo.Matches AS existing WITH (UPDLOCK, HOLDLOCK)
                            ON existing.Id <> candidate.Id
                           AND existing.ScheduledAt = candidate.ScheduledAt
                        WHERE candidate.HomeTeamId IN (existing.HomeTeamId, existing.AwayTeamId)
                           OR candidate.AwayTeamId IN (existing.HomeTeamId, existing.AwayTeamId)
                    )
                    BEGIN
                        THROW 51001, 'Matches.ScheduleConflict', 1;
                    END
                END;
                """);
        }
    }
}
