// Responsabilidad del archivo: Verifica la composición mínima y la repetibilidad declarada del seed FIFA 2026.
// Relación en el sistema: Inspecciona las operaciones de la migración antes de que SQL Server las aplique al entorno real.
using System.Text.RegularExpressions;
using DotNetTestMundial.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DotNetTestMundial.Infrastructure.Tests;

public sealed class WorldCup2026SeedMigrationTests
{
    [Fact]
    public void Up_ContainsRequiredTournamentDataset()
    {
        var migration = new SeedWorldCup2026Tournament();
        var sql = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;
        var teamRows = Segment(sql, "INSERT @TeamSeed VALUES", "INSERT dbo.Teams");
        var playerRows = Segment(sql, "INSERT @PlayerSeed VALUES", "INSERT dbo.Players");
        var matchRows = Segment(sql, "INSERT @MatchSeed VALUES", "INSERT dbo.Matches");
        var goalRows = Segment(sql, "INSERT @GoalSeed VALUES", "INSERT dbo.Goals");

        Assert.Equal(4, Regex.Matches(teamRows, "'26000000-").Count);
        Assert.Equal(20, Regex.Matches(playerRows, "'26[1-4]00000-").Count);
        Assert.Equal(6, Regex.Matches(matchRows, "'26500000-").Count);
        Assert.Equal(3, Regex.Matches(matchRows, @", 2, \d+, \d+\)").Count);
        Assert.Equal(6, Regex.Matches(goalRows, "'26600000-").Count);
    }

    [Fact]
    public void Up_ReusesExistingTeamsAndPlayersBeforeInserting()
    {
        var migration = new SeedWorldCup2026Tournament();
        var sql = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;

        Assert.Contains("WHERE t.ShortName = s.ShortName", sql, StringComparison.Ordinal);
        Assert.Contains("p.TeamId = team.Id AND p.Name = s.Name", sql, StringComparison.Ordinal);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM dbo.Matches", sql, StringComparison.Ordinal);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM dbo.Goals", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_RemovesSeedRowsInForeignKeyOrder()
    {
        var migration = new SeedWorldCup2026Tournament();
        var sql = Assert.Single(migration.DownOperations.OfType<SqlOperation>()).Sql;

        Assert.True(sql.IndexOf("DELETE FROM dbo.Goals", StringComparison.Ordinal) <
                    sql.IndexOf("DELETE m FROM dbo.Matches", StringComparison.Ordinal));
        Assert.True(sql.IndexOf("DELETE m FROM dbo.Matches", StringComparison.Ordinal) <
                    sql.IndexOf("DELETE p FROM dbo.Players", StringComparison.Ordinal));
        Assert.True(sql.IndexOf("DELETE p FROM dbo.Players", StringComparison.Ordinal) <
                    sql.IndexOf("DELETE t FROM dbo.Teams", StringComparison.Ordinal));
    }

    private static string Segment(string sql, string start, string end)
    {
        var first = sql.IndexOf(start, StringComparison.Ordinal);
        var last = sql.IndexOf(end, first + start.Length, StringComparison.Ordinal);
        Assert.True(first >= 0 && last > first);
        return sql[first..last];
    }
}
