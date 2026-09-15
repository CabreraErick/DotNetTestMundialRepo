// Responsabilidad del archivo: Verifica programación, equipos existentes e idempotencia persistente de partidos.
// Relación en el sistema: Usa dobles de Dapper, EF, IdempotencyStore y Unit of Work para aislar Application.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Matches.CreateMatch;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Matches.Results;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Tests.Matches;

public sealed class CreateMatchCommandHandlerTests
{
    [Fact]
    public async Task ValidCommand_StagesMatchAndResponseThenCommits()
    {
        var fixture = new Fixture();

        var result = await fixture.Handler.HandleAsync(fixture.Command("match-key-1"));

        Assert.True(result.IsSuccess);
        var match = Assert.Single(fixture.Writes.Added);
        Assert.Equal(fixture.HomeTeamId, match.HomeTeamId);
        Assert.Equal(fixture.AwayTeamId, match.AwayTeamId);
        Assert.Equal(match.Id, result.Value.MatchId);
        Assert.Single(fixture.Store.Staged);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task MissingAwayTeam_ReturnsNotFoundWithoutWriting()
    {
        var fixture = new Fixture(includeAway: false);

        var result = await fixture.Handler.HandleAsync(fixture.Command("match-key-1"));

        Assert.Equal(MatchMutationErrors.AwayTeamNotFound, result.Error);
        Assert.Empty(fixture.Writes.Added);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task IdenticalTeams_ReturnDomainValidationBeforeReading()
    {
        var fixture = new Fixture();

        var result = await fixture.Handler.HandleAsync(new(
            fixture.HomeTeamId, fixture.HomeTeamId, fixture.Date, "match-key-1"));

        Assert.Equal(DomainErrors.SameTeams, result.Error);
        Assert.Equal(0, fixture.Teams.FindCalls);
    }

    [Fact]
    public async Task SameKeyAndRequest_ReplaysOriginalResponse()
    {
        var fixture = new Fixture();
        var first = await fixture.Handler.HandleAsync(fixture.Command("match-key-1"));
        fixture.Store.PublishStaged();

        var replay = await fixture.Handler.HandleAsync(fixture.Command("match-key-1"));

        Assert.True(replay.Value.IsReplay);
        Assert.Equal(first.Value.ResponseBody, replay.Value.ResponseBody);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task SameKeyAndDifferentRequest_ReturnsConflict()
    {
        var fixture = new Fixture();
        await fixture.Handler.HandleAsync(fixture.Command("match-key-1"));
        fixture.Store.PublishStaged();

        var result = await fixture.Handler.HandleAsync(new(
            fixture.HomeTeamId, fixture.AwayTeamId, fixture.Date.AddHours(1), "match-key-1"));

        Assert.Equal(IdempotencyErrors.KeyReused, result.Error);
        Assert.Single(fixture.Writes.Added);
    }

    [Fact]
    public async Task ConcurrentKeyLoss_ReplaysCommittedWinner()
    {
        var fixture = new Fixture(
            commit: Result<int>.Failure(PersistenceErrors.ConstraintViolation));
        fixture.Store.PublishOnSecondFind = true;

        var result = await fixture.Handler.HandleAsync(fixture.Command("match-key-1"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
    }

    [Fact]
    public async Task TeamAlreadyScheduledOnSameDay_ReturnsTypedConflict()
    {
        var fixture = new Fixture(scheduleConflict: true);

        var result = await fixture.Handler.HandleAsync(fixture.Command("match-key-1"));

        Assert.Equal(MatchMutationErrors.ScheduleConflict, result.Error);
        Assert.Empty(fixture.Writes.Added);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    private sealed class Fixture
    {
        public Guid HomeTeamId { get; } = Guid.NewGuid();
        public Guid AwayTeamId { get; } = Guid.NewGuid();
        public DateTime Date { get; } = new(2026, 9, 20, 15, 0, 0);
        public TeamReadRepository Teams { get; }
        public MatchWriteRepository Writes { get; } = new();
        public StubUnitOfWork UnitOfWork { get; }
        public StubStore Store { get; } = new();
        public CreateMatchCommandHandler Handler { get; }

        public Fixture(bool includeAway = true, Result<int>? commit = null, bool scheduleConflict = false)
        {
            Teams = new(HomeTeamId, includeAway ? AwayTeamId : null);
            UnitOfWork = new(commit ?? Result<int>.Success(2));
            Handler = new(new MatchTeamValidator(Teams, new MatchReadRepository(scheduleConflict)), Writes, UnitOfWork, Store);
        }

        public CreateMatchCommand Command(string key) =>
            new(HomeTeamId, AwayTeamId, Date, key);
    }

    private sealed class MatchReadRepository(bool scheduleConflict) : IMatchReadRepository
    {
        public Task<bool> HasTeamScheduleConflictAsync(
            Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt,
            Guid? excludingMatchId = null, CancellationToken token = default) => Task.FromResult(scheduleConflict);
        public Task<MatchListItem?> FindByIdAsync(Guid id, CancellationToken token = default) => throw new NotSupportedException();
        public Task<PagedResult<MatchListItem>> GetPageAsync(MatchPageSpecification specification, CancellationToken token = default) => throw new NotSupportedException();
        public Task<MatchGoalsPage> GetGoalsAsync(GoalPageSpecification specification, CancellationToken token = default) => throw new NotSupportedException();
        public Task<MatchStateSnapshot?> FindStateByIdAsync(Guid matchId, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class TeamReadRepository(Guid homeId, Guid? awayId) : ITeamReadRepository
    {
        public Task<TeamIdentityConflict> FindIdentityConflictAsync(
            string name, string shortName, Guid? excludingId = null,
            CancellationToken token = default) => throw new NotSupportedException();
        public int FindCalls { get; private set; }
        public Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            FindCalls++;
            return Task.FromResult<TeamListItem?>(id == homeId || id == awayId
                ? new(id, $"Team {id}", "T")
                : null);
        }
        public Task<PagedResult<TeamListItem>> GetPageAsync(
            TeamPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class MatchWriteRepository : IWriteRepository<Match>
    {
        public List<Match> Added { get; } = [];
        public void Add(Match entity) => Added.Add(entity);
        public void Update(Match entity) => throw new NotSupportedException();
        public void Remove(Match entity) => throw new NotSupportedException();
    }

    private sealed class StubUnitOfWork(Result<int> result) : IUnitOfWork
    {
        public int CommitCalls { get; private set; }
        public Task<Result<int>> CommitAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            CommitCalls++;
            return Task.FromResult(result);
        }
        public void Rollback() { }
    }

    private sealed class StubStore : IIdempotencyStore
    {
        private StoredIdempotentResponse? _visible;
        private int _finds;
        public bool PublishOnSecondFind { get; set; }
        public List<StoredIdempotentResponse> Staged { get; } = [];
        public Task<StoredIdempotentResponse?> FindAsync(
            string operation, string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (PublishOnSecondFind && ++_finds == 2)
                PublishStaged();
            return Task.FromResult(_visible);
        }
        public void Stage(StoredIdempotentResponse response) => Staged.Add(response);
        public void PublishStaged() => _visible = Staged.Single();
    }
}
