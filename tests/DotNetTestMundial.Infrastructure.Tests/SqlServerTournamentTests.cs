// Real SQL Server tests: use production migrations, EF/UnitOfWork writes and Dapper reads.
// The fixture owns one GUID-named database and never deletes the application database.
using Dapper;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Results;
using DotNetTestMundial.Application.Tournament.Queries;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetTestMundial.Infrastructure.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("QA_SQLSERVER_CONNECTION")))
            Skip = "Set QA_SQLSERVER_CONNECTION or run scripts/Test-Qa.ps1 for real SQL tests.";
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("QA_SQLSERVER_CONNECTION")))
            Skip = "Set QA_SQLSERVER_CONNECTION or run scripts/Test-Qa.ps1 for real SQL tests.";
    }
}

[Trait("Category", "SqlServer")]
public sealed class SqlServerTournamentTests(SqlServerTournamentFixture fixture)
    : IClassFixture<SqlServerTournamentFixture>
{
    [SqlServerTheory]
    [InlineData("Argentina", 2, 1, 0, 1, 2, 2, 0, 3)]
    [InlineData("Brasil", 1, 0, 0, 1, 1, 2, -1, 0)]
    [InlineData("Francia", 2, 1, 1, 0, 2, 1, 1, 4)]
    [InlineData("España", 1, 0, 1, 0, 1, 1, 0, 1)]
    public async Task Standings_CalculatePlayedWinsDrawsLossesGoalsAndPoints(string name,
        long played, long won, long drawn, long lost, long goalsFor, long goalsAgainst,
        long difference, long points)
    {
        using var scope = fixture.Provider.CreateScope();
        var page = await scope.ServiceProvider.GetRequiredService<ITournamentReadRepository>()
            .GetStandingsAsync(new(name, 1, 10, StandingSortField.Points, TournamentSortDirection.Descending));
        var row = Assert.Single(page.Data);
        Assert.Equal(played, row.Played);
        Assert.Equal(won, row.Won);
        Assert.Equal(drawn, row.Drawn);
        Assert.Equal(lost, row.Lost);
        Assert.Equal(goalsFor, row.GoalsFor);
        Assert.Equal(goalsAgainst, row.GoalsAgainst);
        Assert.Equal(difference, row.GoalDifference);
        Assert.Equal(points, row.Points);
    }

    [SqlServerFact]
    public async Task Standings_BreakTiesByDifferenceThenGoalsFor()
    {
        using var scope = fixture.Provider.CreateScope();
        var page = await scope.ServiceProvider.GetRequiredService<ITournamentReadRepository>()
            .GetStandingsAsync(new("RubricTie", 1, 100, StandingSortField.Points, TournamentSortDirection.Descending));
        Assert.Equal(new[] { "RubricTie E", "RubricTie B", "RubricTie A" },
            page.Data.Take(3).Select(row => row.TeamName));
        Assert.All(page.Data.Take(3), row => Assert.Equal(3, row.Points));
        Assert.Equal(2, page.Data[0].GoalDifference);
        Assert.Equal(1, page.Data[1].GoalDifference);
        Assert.Equal(2, page.Data[1].GoalsFor);
        Assert.Equal(1, page.Data[2].GoalsFor);
    }

    [SqlServerFact]
    public async Task Standings_IgnoreScheduledAndCancelledMatches()
    {
        using var scope = fixture.Provider.CreateScope();
        var page = await scope.ServiceProvider.GetRequiredService<ITournamentReadRepository>()
            .GetStandingsAsync(new("RubricUnplayed", 1, 100, StandingSortField.Points, TournamentSortDirection.Descending));
        Assert.Equal(4, page.Data.Count);
        Assert.All(page.Data, row =>
        {
            Assert.Equal(0, row.Played);
            Assert.Equal(0, row.Points);
            Assert.Equal(0, row.GoalsFor);
        });
        var scorers = await scope.ServiceProvider.GetRequiredService<ITournamentReadRepository>()
            .GetScorersAsync(new("RubricUnplayed", null, 1, 100, ScorerSortField.Goals, TournamentSortDirection.Descending));
        Assert.Empty(scorers.Data);
    }

    [SqlServerFact]
    public async Task Standings_PaginateInDatabaseWithoutRepeatingRows()
    {
        using var scope = fixture.Provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITournamentReadRepository>();
        var first = await repository.GetStandingsAsync(new("RubricTie", 1, 2, StandingSortField.Points, TournamentSortDirection.Descending));
        var second = await repository.GetStandingsAsync(new("RubricTie", 2, 2, StandingSortField.Points, TournamentSortDirection.Descending));
        Assert.Equal(6, first.TotalRecords);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(2, first.Data.Count);
        Assert.Empty(first.Data.Select(row => row.TeamId).Intersect(second.Data.Select(row => row.TeamId)));
    }

    [SqlServerFact]
    public async Task Goals_PaginateFilterSortAndKeepWholeMatchScore()
    {
        using var scope = fixture.Provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMatchReadRepository>();
        var match = (await repository.FindByIdAsync(Guid.Parse("26500000-0000-0000-0000-000000000001")))!;
        var specification = new GoalPageSpecification(match.Id, match.HomeTeamId, match.AwayTeamId,
            null, null, 2, 1, GoalSortField.Minute, MatchSortDirection.Ascending);
        var page = await repository.GetGoalsAsync(specification);
        Assert.Equal(44, Assert.Single(page.Data).Minute);
        Assert.Equal(3, page.TotalRecords);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, page.HomeGoals);
        Assert.Equal(1, page.AwayGoals);
        var filtered = await repository.GetGoalsAsync(specification with
        {
            PageNumber = 1,
            TeamId = match.HomeTeamId,
            SortDirection = MatchSortDirection.Descending
        });
        Assert.Equal(67, Assert.Single(filtered.Data).Minute);
        Assert.Equal(2, filtered.TotalRecords);
        Assert.Equal(2, filtered.HomeGoals);
        Assert.Equal(1, filtered.AwayGoals);
        var empty = await repository.GetGoalsAsync(specification with { PageNumber = 99 });
        Assert.Empty(empty.Data);
        Assert.Equal(3, empty.TotalRecords);
        var literal = await repository.GetGoalsAsync(specification with { Search = "%" });
        Assert.Empty(literal.Data);
    }

    [SqlServerFact]
    public async Task Startup_ContainsRequiredSeedAndDoesNotDuplicateOnRestart()
    {
        using var scope = fixture.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TournamentDatabaseInitializer>().InitializeAsync();
        await using var connection = new SqlConnection(fixture.ConnectionString);
        Assert.Equal(4, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Teams WHERE ShortName IN ('ARG','BRA','FRA','ESP')"));
        Assert.Equal(20, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Players p INNER JOIN dbo.Teams t ON p.TeamId=t.Id WHERE t.ShortName IN ('ARG','BRA','FRA','ESP')"));
        Assert.Equal(6, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Matches WHERE CONVERT(varchar(36),Id) LIKE '26500000-%'"));
        Assert.Equal(3, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Matches WHERE CONVERT(varchar(36),Id) LIKE '26500000-%' AND Status=2"));
    }
}

public sealed class SqlServerTournamentFixture : IAsyncLifetime
{
    private readonly string _databaseName = "DotNetTestMundial_QA_" + Guid.NewGuid().ToString("N");
    private string? _masterConnection;
    public ServiceProvider Provider { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("QA_SQLSERVER_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured)) return;
        var builder = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master" };
        _masterConnection = builder.ConnectionString;
        builder.InitialCatalog = _databaseName;
        ConnectionString = builder.ConnectionString;
        Provider = new ServiceCollection().AddInfrastructure(ConnectionString).BuildServiceProvider();
        try
        {
            using var scope = Provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<TournamentDatabaseInitializer>().InitializeAsync();
            for (var pair = 0; pair < 5; pair++)
            {
                var prefix = pair < 3 ? "RubricTie" : "RubricUnplayed";
                var suffix = pair < 3 ? pair * 2 : (pair - 3) * 2;
                var homeLetter = pair < 3 ? new[] { 'A', 'B', 'E' }[pair] : (char)('A' + suffix);
                var awayLetter = pair < 3 ? new[] { 'D', 'F', 'C' }[pair] : (char)('B' + suffix);
                var home = Team.Create($"{prefix} {homeLetter}", $"QA{pair}H").Value;
                var away = Team.Create($"{prefix} {awayLetter}", $"QA{pair}A").Value;
                var player = Player.Create(home.Id, prefix + " scorer", 9).Value;
                var awayPlayer = Player.Create(away.Id, prefix + " away scorer", 9).Value;
                var match = Match.Create(home.Id, away.Id, new DateTime(2026, 12, 1).AddDays(pair)).Value;
                var teams = scope.ServiceProvider.GetRequiredService<IWriteRepository<Team>>();
                teams.Add(home); teams.Add(away);
                var players = scope.ServiceProvider.GetRequiredService<IWriteRepository<Player>>();
                players.Add(player); players.Add(awayPlayer);
                var goals = scope.ServiceProvider.GetRequiredService<IWriteRepository<Goal>>();
                var homeScore = pair == 0 ? 1 : 2;
                for (var minute = 1; minute <= homeScore; minute++)
                {
                    var goal = Goal.Create(match.Id, player, minute).Value;
                    Assert.True(match.AddGoal(goal).IsSuccess);
                    goals.Add(goal);
                }
                var awayScore = pair == 1 ? 1 : 0;
                if (awayScore == 1)
                {
                    var goal = Goal.Create(match.Id, awayPlayer, 30).Value;
                    Assert.True(match.AddGoal(goal).IsSuccess);
                    goals.Add(goal);
                }
                if (pair < 3) Assert.True(match.RegisterResult(homeScore, awayScore).IsSuccess);
                if (pair == 4) Assert.True(match.Cancel().IsSuccess);
                scope.ServiceProvider.GetRequiredService<IWriteRepository<Match>>().Add(match);
                Assert.True((await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync()).IsSuccess);
            }
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (Provider is not null) await Provider.DisposeAsync();
        if (_masterConnection is null) return;
        if (!System.Text.RegularExpressions.Regex.IsMatch(_databaseName, "^DotNetTestMundial_QA_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing cleanup outside the owned QA database.");
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(_masterConnection);
        await connection.ExecuteAsync($"IF DB_ID('{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END");
        _masterConnection = null;
    }
}
