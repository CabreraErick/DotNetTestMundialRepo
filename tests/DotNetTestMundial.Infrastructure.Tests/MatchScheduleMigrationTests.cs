// Responsabilidad del archivo: verifica la migración que impide dos partidos diarios por equipo.
// Relación en el sistema: protege la sincronía entre la consulta Dapper, el trigger SQL Server y la limpieza QA.
using DotNetTestMundial.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DotNetTestMundial.Infrastructure.Tests;

public sealed class MatchScheduleMigrationTests
{
    [Fact]
    public void Up_UsesWholeDayAndIgnoresCancelledMatches()
    {
        var migration = new PreventSameDayMatchesAndSupportTriggers();
        var sql = string.Join(Environment.NewLine, migration.UpOperations.OfType<SqlOperation>().Select(x => x.Sql));

        Assert.Contains("existing.ScheduledAt >= CONVERT(date, candidate.ScheduledAt)", sql);
        Assert.Contains("DATEADD(day, 1, CONVERT(date, candidate.ScheduledAt))", sql);
        Assert.Contains("existing.Status <> 3", sql);
        Assert.Contains("candidate.Status <> 3", sql);
        Assert.Contains("THROW 51001, 'Matches.ScheduleConflict'", sql);
    }

    [Fact]
    public void Up_RemovesOnlyTheVerifiedRedundantQaMatch()
    {
        var migration = new PreventSameDayMatchesAndSupportTriggers();
        var sql = string.Join(Environment.NewLine, migration.UpOperations.OfType<SqlOperation>().Select(x => x.Sql));

        Assert.Contains("home.Name = N'Colombia'", sql);
        Assert.Contains("away.Name = N'España'", sql);
        Assert.Contains("redundant.Status = 1", sql);
        Assert.Contains("NOT EXISTS (SELECT 1 FROM dbo.Goals", sql);
        Assert.Contains("retainedHome.Name = N'España'", sql);
        Assert.Contains("retainedAway.Name = N'Egipto'", sql);
    }
}
