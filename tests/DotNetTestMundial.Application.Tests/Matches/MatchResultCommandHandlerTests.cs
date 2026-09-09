// Responsabilidad del archivo: Verifica registro idempotente de goles, consulta Dapper y cierre coherente del marcador.
// Relación en el sistema: Usa dobles de los puertos para probar Application y las reglas Domain sin depender de SQL Server.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Matches.Results;
using DotNetTestMundial.Application.Players.GetPlayers;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Tests.Matches;

public sealed class MatchResultCommandHandlerTests
{
    [Fact]
    public async Task CreateGoal_ValidScorer_StagesGoalAndResponseThenCommits()
    {
        var fixture = new Fixture();

        var result = await fixture.CreateGoal.HandleAsync(new(
            fixture.Match.Id, fixture.HomePlayer.Id, 23, "goal-key-1"));

        Assert.True(result.IsSuccess);
        var goal = Assert.Single(fixture.GoalWrites.Added);
        Assert.Equal(fixture.HomePlayer.Id, goal.PlayerId);
        Assert.Equal(fixture.Match.HomeTeamId, goal.TeamId);
        Assert.Single(fixture.Store.Staged);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task CreateGoal_InactivePlayer_ReturnsConflictWithoutWriting()
    {
        var fixture = new Fixture(homePlayerActive: false);

        var result = await fixture.CreateGoal.HandleAsync(new(
            fixture.Match.Id, fixture.HomePlayer.Id, 23, "goal-key-1"));

        Assert.Equal(DomainErrors.InactivePlayer, result.Error);
        Assert.Empty(fixture.GoalWrites.Added);
    }

    [Fact]
    public async Task CreateGoal_PlayerFromAnotherTeam_IsRejectedByMatch()
    {
        var fixture = new Fixture(playerTeamId: Guid.NewGuid());

        var result = await fixture.CreateGoal.HandleAsync(new(
            fixture.Match.Id, fixture.HomePlayer.Id, 23, "goal-key-1"));

        Assert.Equal(DomainErrors.ScorerTeamMismatch, result.Error);
        Assert.Empty(fixture.GoalWrites.Added);
    }

    [Fact]
    public async Task CreateGoal_SameKeyAndRequest_ReplaysOriginalResponse()
    {
        var fixture = new Fixture();
        var command = new CreateGoalCommand(
            fixture.Match.Id, fixture.HomePlayer.Id, 23, "goal-key-1");
        var first = await fixture.CreateGoal.HandleAsync(command);
        fixture.Store.PublishStaged();

        var replay = await fixture.CreateGoal.HandleAsync(command);

        Assert.True(replay.Value.IsReplay);
        Assert.Equal(first.Value.ResponseBody, replay.Value.ResponseBody);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task CreateGoal_SameKeyAndDifferentMinute_ReturnsConflict()
    {
        var fixture = new Fixture();
        await fixture.CreateGoal.HandleAsync(new(
            fixture.Match.Id, fixture.HomePlayer.Id, 23, "goal-key-1"));
        fixture.Store.PublishStaged();

        var result = await fixture.CreateGoal.HandleAsync(new(
            fixture.Match.Id, fixture.HomePlayer.Id, 24, "goal-key-1"));

        Assert.Equal("Idempotency.KeyReused", result.Error!.Code);
        Assert.Single(fixture.GoalWrites.Added);
    }

    [Fact]
    public async Task CreateGoal_MissingPlayer_ReturnsNotFoundWithoutCommit()
    {
        var fixture = new Fixture(includePlayer: false);

        var result = await fixture.CreateGoal.HandleAsync(new(
            fixture.Match.Id, fixture.HomePlayer.Id, 23, "goal-key-1"));

        Assert.Equal(MatchResultErrors.PlayerNotFound, result.Error);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task CreateGoal_ConcurrentKeyLoss_ReplaysCommittedWinner()
    {
        var fixture = new Fixture(
            commit: Result<int>.Failure(PersistenceErrors.ConstraintViolation));
        fixture.Store.PublishOnSecondFind = true;

        var result = await fixture.CreateGoal.HandleAsync(new(
            fixture.Match.Id, fixture.HomePlayer.Id, 23, "goal-key-1"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
    }

    [Fact]
    public async Task RegisterResult_MatchingPersistedGoals_PlaysMatchAndCommits()
    {
        var fixture = new Fixture();
        fixture.Reads.Goals.Add(new(
            Guid.NewGuid(), fixture.Match.Id, fixture.HomePlayer.Id, fixture.HomePlayer.Name,
            fixture.Match.HomeTeamId, "Local", 23));

        var result = await fixture.Result.HandleAsync(new(fixture.Match.Id, 1, 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Played, result.Value.Status);
        var updated = Assert.Single(fixture.MatchWrites.Updated);
        Assert.Equal(1, updated.HomeScore);
        Assert.Equal(0, updated.AwayScore);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task RegisterResult_ScoreDifferentFromGoals_ReturnsValidationError()
    {
        var fixture = new Fixture();

        var result = await fixture.Result.HandleAsync(new(fixture.Match.Id, 1, 0));

        Assert.Equal(DomainErrors.ScoreMismatch, result.Error);
        Assert.Empty(fixture.MatchWrites.Updated);
    }

    [Fact]
    public async Task GetGoals_ExistingMatch_ReturnsChronologicalProjection()
    {
        var fixture = new Fixture();
        fixture.Reads.Goals.Add(new(
            Guid.NewGuid(), fixture.Match.Id, fixture.HomePlayer.Id, fixture.HomePlayer.Name,
            fixture.Match.HomeTeamId, "Local", 23));

        var result = await new GetMatchGoalsQueryHandler(fixture.Reads)
            .HandleAsync(new(fixture.Match.Id));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    private sealed class Fixture
    {
        public MatchListItem Match { get; }
        public PlayerListItem HomePlayer { get; }
        public MatchReadRepository Reads { get; }
        public PlayerReadRepository Players { get; }
        public GoalWriteRepository GoalWrites { get; } = new();
        public MatchWriteRepository MatchWrites { get; } = new();
        public StubUnitOfWork UnitOfWork { get; }
        public StubStore Store { get; } = new();
        public CreateGoalCommandHandler CreateGoal { get; }
        public RegisterMatchResultCommandHandler Result { get; }

        public Fixture(
            bool homePlayerActive = true,
            Guid? playerTeamId = null,
            bool includePlayer = true,
            Result<int>? commit = null)
        {
            Match = new(Guid.NewGuid(), Guid.NewGuid(), "Local", Guid.NewGuid(), "Visitante",
                new DateTime(2026, 9, 20, 15, 0, 0), MatchStatus.Scheduled, null, null, 0);
            HomePlayer = new(Guid.NewGuid(), playerTeamId ?? Match.HomeTeamId,
                "Goleador", 10, homePlayerActive);
            Reads = new(Match);
            Players = new(includePlayer ? HomePlayer : null);
            UnitOfWork = new(commit ?? Result<int>.Success(1));
            CreateGoal = new(Reads, Players, GoalWrites, UnitOfWork, Store);
            Result = new(Reads, MatchWrites, UnitOfWork);
        }
    }

    private sealed class MatchReadRepository(MatchListItem match) : IMatchReadRepository
    {
        public List<GoalListItem> Goals { get; } = [];
        public Task<MatchListItem?> FindByIdAsync(Guid id, CancellationToken token = default) =>
            Task.FromResult<MatchListItem?>(id == match.Id ? match : null);
        public Task<IReadOnlyList<GoalListItem>> GetGoalsAsync(
            Guid matchId, CancellationToken token = default) =>
            Task.FromResult<IReadOnlyList<GoalListItem>>(Goals);
        public Task<MatchStateSnapshot?> FindStateByIdAsync(
            Guid matchId, CancellationToken token = default) =>
            Task.FromResult<MatchStateSnapshot?>(matchId == match.Id
                ? new(match with { GoalCount = Goals.Count }, Goals)
                : null);
        public Task<PagedResult<MatchListItem>> GetPageAsync(
            MatchPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class PlayerReadRepository(PlayerListItem? player) : IPlayerReadRepository
    {
        public Task<PlayerListItem?> FindByIdAsync(Guid id, CancellationToken token = default) =>
            Task.FromResult<PlayerListItem?>(id == player?.Id ? player : null);
        public Task<PagedResult<PlayerListItem>> GetPageAsync(
            PlayerPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class GoalWriteRepository : IWriteRepository<Goal>
    {
        public List<Goal> Added { get; } = [];
        public void Add(Goal entity) => Added.Add(entity);
        public void Update(Goal entity) => throw new NotSupportedException();
        public void Remove(Goal entity) => throw new NotSupportedException();
    }

    private sealed class MatchWriteRepository : IWriteRepository<Match>
    {
        public List<Match> Updated { get; } = [];
        public void Add(Match entity) => throw new NotSupportedException();
        public void Update(Match entity) => Updated.Add(entity);
        public void Remove(Match entity) => throw new NotSupportedException();
    }

    private sealed class StubUnitOfWork(Result<int> result) : IUnitOfWork
    {
        public int CommitCalls { get; private set; }
        public Task<Result<int>> CommitAsync(CancellationToken token = default)
        {
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
