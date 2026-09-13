using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// Responsabilidad del archivo: instala restricciones persistentes para identidades, dorsales y horarios únicos.
// Relación en el sistema: protege SQL Server incluso ante escrituras concurrentes o clientes fuera del frontend.

namespace DotNetTestMundial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceQaBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "Teams",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Players",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "UX_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Teams_ShortName",
                table: "Teams",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Players_TeamId_JerseyNumber",
                table: "Players",
                columns: new[] { "TeamId", "JerseyNumber" },
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_Matches_RejectTeamScheduleConflict;");

            migrationBuilder.DropIndex(
                name: "UX_Teams_Name",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "UX_Teams_ShortName",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "UX_Players_TeamId_JerseyNumber",
                table: "Players");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);
        }
    }
}
