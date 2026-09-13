using Microsoft.EntityFrameworkCore.Migrations;

// Responsabilidad del archivo: Inserta de forma idempotente el torneo demostrativo basado en plantillas FIFA 2026.
// Relación en el sistema: SQL Server aplica equipos, jugadores, partidos y goles coherentes al ejecutar las migraciones EF Core.
#nullable disable

namespace DotNetTestMundial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedWorldCup2026Tournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @TeamSeed TABLE
                (
                    SeedId uniqueidentifier NOT NULL,
                    Name nvarchar(100) NOT NULL,
                    ShortName nvarchar(10) NOT NULL
                );
                INSERT @TeamSeed VALUES
                    ('26000000-0000-0000-0000-000000000001', N'Argentina', N'ARG'),
                    ('26000000-0000-0000-0000-000000000002', N'Brasil', N'BRA'),
                    ('26000000-0000-0000-0000-000000000003', N'Francia', N'FRA'),
                    ('26000000-0000-0000-0000-000000000004', N'España', N'ESP');

                INSERT dbo.Teams (Id, Name, ShortName)
                SELECT s.SeedId, s.Name, s.ShortName
                FROM @TeamSeed s
                WHERE NOT EXISTS (SELECT 1 FROM dbo.Teams t WHERE t.ShortName = s.ShortName)
                  AND NOT EXISTS (SELECT 1 FROM dbo.Teams t WHERE t.Id = s.SeedId);

                DECLARE @PlayerSeed TABLE
                (
                    SeedId uniqueidentifier NOT NULL,
                    TeamCode nvarchar(10) NOT NULL,
                    Name nvarchar(100) NOT NULL,
                    JerseyNumber int NOT NULL
                );
                INSERT @PlayerSeed VALUES
                    ('26100000-0000-0000-0000-000000000001', N'ARG', N'Juan Musso', 1),
                    ('26100000-0000-0000-0000-000000000002', N'ARG', N'Rodrigo De Paul', 7),
                    ('26100000-0000-0000-0000-000000000003', N'ARG', N'Julián Álvarez', 9),
                    ('26100000-0000-0000-0000-000000000004', N'ARG', N'Lionel Messi', 10),
                    ('26100000-0000-0000-0000-000000000005', N'ARG', N'Cristian Romero', 13),
                    ('26200000-0000-0000-0000-000000000001', N'BRA', N'Alisson Becker', 1),
                    ('26200000-0000-0000-0000-000000000002', N'BRA', N'Marquinhos', 4),
                    ('26200000-0000-0000-0000-000000000003', N'BRA', N'Vinícius Júnior', 7),
                    ('26200000-0000-0000-0000-000000000004', N'BRA', N'Bruno Guimarães', 8),
                    ('26200000-0000-0000-0000-000000000005', N'BRA', N'Neymar Jr', 10),
                    ('26300000-0000-0000-0000-000000000001', N'FRA', N'Brice Samba', 1),
                    ('26300000-0000-0000-0000-000000000002', N'FRA', N'Dayot Upamecano', 4),
                    ('26300000-0000-0000-0000-000000000003', N'FRA', N'Ousmane Dembélé', 7),
                    ('26300000-0000-0000-0000-000000000004', N'FRA', N'Aurélien Tchouaméni', 8),
                    ('26300000-0000-0000-0000-000000000005', N'FRA', N'Kylian Mbappé', 10),
                    ('26400000-0000-0000-0000-000000000001', N'ESP', N'David Raya', 1),
                    ('26400000-0000-0000-0000-000000000002', N'ESP', N'Mikel Merino', 6),
                    ('26400000-0000-0000-0000-000000000003', N'ESP', N'Lamine Yamal', 19),
                    ('26400000-0000-0000-0000-000000000004', N'ESP', N'Pedri', 20),
                    ('26400000-0000-0000-0000-000000000005', N'ESP', N'Mikel Oyarzabal', 21);

                INSERT dbo.Players (Id, TeamId, Name, JerseyNumber, IsActive)
                SELECT s.SeedId, team.Id, s.Name, s.JerseyNumber, 1
                FROM @PlayerSeed s
                CROSS APPLY
                (
                    SELECT TOP (1) t.Id FROM dbo.Teams t
                    WHERE t.ShortName = s.TeamCode ORDER BY t.Id
                ) team
                WHERE NOT EXISTS
                    (SELECT 1 FROM dbo.Players p
                     WHERE p.TeamId = team.Id AND p.Name = s.Name
                       AND p.JerseyNumber = s.JerseyNumber AND p.IsActive = 1)
                  AND NOT EXISTS (SELECT 1 FROM dbo.Players p WHERE p.Id = s.SeedId);

                DECLARE @MatchSeed TABLE
                (
                    SeedId uniqueidentifier NOT NULL,
                    HomeCode nvarchar(10) NOT NULL,
                    AwayCode nvarchar(10) NOT NULL,
                    ScheduledAt datetime2 NOT NULL,
                    Status int NOT NULL,
                    HomeScore int NULL,
                    AwayScore int NULL
                );
                INSERT @MatchSeed VALUES
                    ('26500000-0000-0000-0000-000000000001', N'ARG', N'BRA', '2026-09-20T18:00:00', 2, 2, 1),
                    ('26500000-0000-0000-0000-000000000002', N'FRA', N'ESP', '2026-09-21T18:00:00', 2, 1, 1),
                    ('26500000-0000-0000-0000-000000000003', N'ARG', N'FRA', '2026-09-22T18:00:00', 2, 0, 1),
                    ('26500000-0000-0000-0000-000000000004', N'BRA', N'ESP', '2026-09-23T18:00:00', 1, NULL, NULL),
                    ('26500000-0000-0000-0000-000000000005', N'ARG', N'ESP', '2026-09-24T18:00:00', 1, NULL, NULL),
                    ('26500000-0000-0000-0000-000000000006', N'BRA', N'FRA', '2026-09-25T18:00:00', 1, NULL, NULL);

                INSERT dbo.Matches
                    (Id, HomeTeamId, AwayTeamId, ScheduledAt, Status, HomeScore, AwayScore)
                SELECT s.SeedId, home.Id, away.Id, s.ScheduledAt, s.Status, s.HomeScore, s.AwayScore
                FROM @MatchSeed s
                CROSS APPLY (SELECT TOP (1) Id FROM dbo.Teams WHERE ShortName = s.HomeCode ORDER BY Id) home
                CROSS APPLY (SELECT TOP (1) Id FROM dbo.Teams WHERE ShortName = s.AwayCode ORDER BY Id) away
                WHERE NOT EXISTS (SELECT 1 FROM dbo.Matches m WHERE m.Id = s.SeedId);

                DECLARE @GoalSeed TABLE
                (
                    SeedId uniqueidentifier NOT NULL,
                    MatchId uniqueidentifier NOT NULL,
                    TeamCode nvarchar(10) NOT NULL,
                    PlayerName nvarchar(100) NOT NULL,
                    JerseyNumber int NOT NULL,
                    Minute int NOT NULL
                );
                INSERT @GoalSeed VALUES
                    ('26600000-0000-0000-0000-000000000001', '26500000-0000-0000-0000-000000000001', N'ARG', N'Lionel Messi', 10, 12),
                    ('26600000-0000-0000-0000-000000000002', '26500000-0000-0000-0000-000000000001', N'ARG', N'Lionel Messi', 10, 67),
                    ('26600000-0000-0000-0000-000000000003', '26500000-0000-0000-0000-000000000001', N'BRA', N'Vinícius Júnior', 7, 44),
                    ('26600000-0000-0000-0000-000000000004', '26500000-0000-0000-0000-000000000002', N'FRA', N'Kylian Mbappé', 10, 35),
                    ('26600000-0000-0000-0000-000000000005', '26500000-0000-0000-0000-000000000002', N'ESP', N'Lamine Yamal', 19, 71),
                    ('26600000-0000-0000-0000-000000000006', '26500000-0000-0000-0000-000000000003', N'FRA', N'Ousmane Dembélé', 7, 58);

                INSERT dbo.Goals (Id, MatchId, PlayerId, TeamId, Minute)
                SELECT s.SeedId, s.MatchId, player.Id, team.Id, s.Minute
                FROM @GoalSeed s
                CROSS APPLY (SELECT TOP (1) Id FROM dbo.Teams WHERE ShortName = s.TeamCode ORDER BY Id) team
                CROSS APPLY
                (
                    SELECT TOP (1) p.Id FROM dbo.Players p
                    WHERE p.TeamId = team.Id AND p.Name = s.PlayerName
                      AND p.JerseyNumber = s.JerseyNumber AND p.IsActive = 1
                    ORDER BY p.Id
                ) player
                WHERE EXISTS (SELECT 1 FROM dbo.Matches m WHERE m.Id = s.MatchId)
                  AND NOT EXISTS (SELECT 1 FROM dbo.Goals g WHERE g.Id = s.SeedId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM dbo.Goals WHERE Id IN
                (
                    '26600000-0000-0000-0000-000000000001', '26600000-0000-0000-0000-000000000002',
                    '26600000-0000-0000-0000-000000000003', '26600000-0000-0000-0000-000000000004',
                    '26600000-0000-0000-0000-000000000005', '26600000-0000-0000-0000-000000000006'
                );

                DELETE m FROM dbo.Matches m
                WHERE m.Id IN
                (
                    '26500000-0000-0000-0000-000000000001', '26500000-0000-0000-0000-000000000002',
                    '26500000-0000-0000-0000-000000000003', '26500000-0000-0000-0000-000000000004',
                    '26500000-0000-0000-0000-000000000005', '26500000-0000-0000-0000-000000000006'
                )
                  AND NOT EXISTS (SELECT 1 FROM dbo.Goals g WHERE g.MatchId = m.Id);

                DELETE p FROM dbo.Players p
                WHERE p.Id IN
                (
                    '26100000-0000-0000-0000-000000000001', '26100000-0000-0000-0000-000000000002',
                    '26100000-0000-0000-0000-000000000003', '26100000-0000-0000-0000-000000000004',
                    '26100000-0000-0000-0000-000000000005', '26200000-0000-0000-0000-000000000001',
                    '26200000-0000-0000-0000-000000000002', '26200000-0000-0000-0000-000000000003',
                    '26200000-0000-0000-0000-000000000004', '26200000-0000-0000-0000-000000000005',
                    '26300000-0000-0000-0000-000000000001', '26300000-0000-0000-0000-000000000002',
                    '26300000-0000-0000-0000-000000000003', '26300000-0000-0000-0000-000000000004',
                    '26300000-0000-0000-0000-000000000005', '26400000-0000-0000-0000-000000000001',
                    '26400000-0000-0000-0000-000000000002', '26400000-0000-0000-0000-000000000003',
                    '26400000-0000-0000-0000-000000000004', '26400000-0000-0000-0000-000000000005'
                )
                  AND NOT EXISTS (SELECT 1 FROM dbo.Goals g WHERE g.PlayerId = p.Id);

                DELETE t FROM dbo.Teams t
                WHERE t.Id IN
                (
                    '26000000-0000-0000-0000-000000000001', '26000000-0000-0000-0000-000000000002',
                    '26000000-0000-0000-0000-000000000003', '26000000-0000-0000-0000-000000000004'
                )
                  AND NOT EXISTS (SELECT 1 FROM dbo.Players p WHERE p.TeamId = t.Id)
                  AND NOT EXISTS
                    (SELECT 1 FROM dbo.Matches m WHERE m.HomeTeamId = t.Id OR m.AwayTeamId = t.Id);
                """);
        }
    }
}
