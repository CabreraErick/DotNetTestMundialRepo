// Responsabilidad del archivo: Verifica PUT, PATCH y cancelación DELETE de partidos programados.
// Relación en el sistema: Aísla Dapper, validación de equipos, EF y Unit of Work para probar la orquestación.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Matches.Results;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Tests.Matches;

public sealed class MatchMutationCommandHandlerTests
{
    [Fact]
    public async Task Update_ScheduledMatch_ReplacesScheduleAndCommits()
    {
        var fixture = new Fixture(ScheduledMatch());
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        fixture.Teams.Add(home, away);
        var date = new DateTime(2026, 10, 1, 18, 0, 0);

        var result = await fixture.Update.HandleAsync(new(fixture.MatchId, home, away, date));

        Assert.True(result.IsSuccess);
        var updated = Assert.Single(fixture.Writes.Updated);
        Assert.Equal(home, updated.HomeTeamId);
        Assert.Equal(away, updated.AwayTeamId);
        Assert.Equal(date, updated.ScheduledAt);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Update_PlayedMatch_ReturnsConflictBeforeTeamLookup()
    {
        var fixture = new Fixture(ScheduledMatch() with
        {
            Status = MatchStatus.Played,
            HomeScore = 0,
            AwayScore = 0
        });

        var result = await fixture.Update.HandleAsync(new(
            fixture.MatchId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));

        Assert.Equal(DomainErrors.MatchNotScheduled, result.Error);
        Assert.Equal(0, fixture.Teams.FindCalls);
        Assert.Empty(fixture.Writes.Updated);
    }

    [Fact]
    public async Task Update_ChangingTeamsAfterGoal_ReturnsConflict()
    {
        var fixture = new Fixture(ScheduledMatch() with { GoalCount = 1 });

        var result = await fixture.Update.HandleAsync(new(
            fixture.MatchId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));

        Assert.Equal(MatchMutationErrors.TeamsLockedByGoals, result.Error);
        Assert.Equal(0, fixture.Teams.FindCalls);
    }

    [Fact]
    public async Task Patch_DateOnly_PreservesTeams()
    {
        var match = ScheduledMatch();
        var fixture = new Fixture(match);
        fixture.Teams.Add(match.HomeTeamId, match.AwayTeamId);
        var date = match.ScheduledAt.AddDays(1);

        var result = await fixture.Patch.HandleAsync(new(match.Id, null, null, date));

        Assert.True(result.IsSuccess);
        Assert.Equal(match.HomeTeamId, result.Value.HomeTeamId);
        Assert.Equal(match.AwayTeamId, result.Value.AwayTeamId);
        Assert.Equal(date, result.Value.ScheduledAt);
    }

    [Fact]
    public async Task Patch_EmptyRequest_StopsBeforeRead()
    {
        var fixture = new Fixture(ScheduledMatch());

        var result = await fixture.Patch.HandleAsync(new(fixture.MatchId, null, null, null));

        Assert.Equal(MatchMutationErrors.PatchEmpty, result.Error);
        Assert.Equal(0, fixture.Reads.FindCalls);
    }

    [Fact]
    public async Task Delete_ScheduledMatch_CancelsAndCommits()
    {
        var fixture = new Fixture(ScheduledMatch());

        var result = await fixture.Delete.HandleAsync(new(fixture.MatchId));

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Cancelled, Assert.Single(fixture.Writes.Updated).Status);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Delete_CancelledMatch_IsRepeatableWithoutCommit()
    {
        var fixture = new Fixture(ScheduledMatch() with { Status = MatchStatus.Cancelled });

        var result = await fixture.Delete.HandleAsync(new(fixture.MatchId));

        Assert.True(result.IsSuccess);
        Assert.Empty(fixture.Writes.Updated);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Delete_PlayedMatch_ReturnsConflict()
    {
        var fixture = new Fixture(ScheduledMatch() with
        {
            Status = MatchStatus.Played,
            HomeScore = 1,
            AwayScore = 0
        });

        var result = await fixture.Delete.HandleAsync(new(fixture.MatchId));

        Assert.Equal(DomainErrors.MatchNotScheduled, result.Error);
        Assert.Empty(fixture.Writes.Updated);
    }

    [Fact]
    public async Task MissingMatch_ReturnsNotFound()
    {
        var fixture = new Fixture(null);
        var result = await fixture.Delete.HandleAsync(new(Guid.NewGuid()));
        Assert.Equal(MatchMutationErrors.NotFound, result.Error);
    }

    private static MatchListItem ScheduledMatch() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Local", Guid.NewGuid(), "Visitante",
        new DateTime(2026, 9, 20, 15, 0, 0), MatchStatus.Scheduled, null, null, 0);

    private sealed class Fixture
    {
        public MatchReadRepository Reads { get; }
        public TeamReadRepository Teams { get; } = new();
        public MatchWriteRepository Writes { get; } = new();
        public StubUnitOfWork UnitOfWork { get; } = new();
        public UpdateMatchCommandHandler Update { get; }
        public PatchMatchCommandHandler Patch { get; }
        public DeleteMatchCommandHandler Delete { get; }
        public Guid MatchId => Reads.Response?.Id ?? Guid.Empty;

        public Fixture(MatchListItem? match)
        {
            Reads = new(match);
            var validator = new MatchTeamValidator(Teams, Reads);
            Update = new(Reads, validator, Writes, UnitOfWork);
            Patch = new(Reads, validator, Writes, UnitOfWork);
            Delete = new(Reads, Writes, UnitOfWork);
        }
    }

    private sealed class MatchReadRepository(MatchListItem? response) : IMatchReadRepository
    {
        public MatchListItem? Response { get; } = response;
        public int FindCalls { get; private set; }
        public Task<MatchListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            FindCalls++;
            return Task.FromResult(Response?.Id == id ? Response : null);
        }
        public Task<bool> HasTeamScheduleConflictAsync(
            Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt,
            Guid? excludingMatchId = null, CancellationToken token = default) => Task.FromResult(false);
        public Task<PagedResult<MatchListItem>> GetPageAsync(
            MatchPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<MatchGoalsPage> GetGoalsAsync(
            GoalPageSpecification specification, CancellationToken token = default) => throw new NotSupportedException();
        public Task<MatchStateSnapshot?> FindStateByIdAsync(
            Guid matchId, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class TeamReadRepository : ITeamReadRepository
    {
        public Task<TeamIdentityConflict> FindIdentityConflictAsync(
            string name, string shortName, Guid? excludingId = null,
            CancellationToken token = default) => throw new NotSupportedException();
        private readonly HashSet<Guid> _ids = [];
        public int FindCalls { get; private set; }
        public void Add(params Guid[] ids) => _ids.UnionWith(ids);
        public Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            FindCalls++;
            return Task.FromResult<TeamListItem?>(_ids.Contains(id)
                ? new(id, $"Team {id}", "T")
                : null);
        }
        public Task<PagedResult<TeamListItem>> GetPageAsync(
            TeamPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class MatchWriteRepository : IWriteRepository<Match>
    {
        public List<Match> Updated { get; } = [];
        public void Add(Match entity) => throw new NotSupportedException();
        public void Update(Match entity) => Updated.Add(entity);
        public void Remove(Match entity) => throw new NotSupportedException();
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int CommitCalls { get; private set; }
        public Task<Result<int>> CommitAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            CommitCalls++;
            return Task.FromResult(Result<int>.Success(1));
        }
        public void Rollback() { }
    }
}
