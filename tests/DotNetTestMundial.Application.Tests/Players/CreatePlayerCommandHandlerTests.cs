// Responsabilidad del archivo: Verifica creación, equipo requerido e idempotencia persistente de jugadores.
// Relación en el sistema: Aísla el handler mediante dobles de los puertos Dapper, EF, idempotencia y Unit of Work.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Players.CreatePlayer;
using DotNetTestMundial.Application.Players.GetPlayers;
using DotNetTestMundial.Application.Players.Mutations;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Tests.Players;

public sealed class CreatePlayerCommandHandlerTests
{
    [Fact]
    public async Task ValidCommand_StagesPlayerAndIdempotentResponseThenCommits()
    {
        var fixture = new Fixture(teamExists: true);

        var result = await fixture.Handler.HandleAsync(
            new(fixture.TeamId, " Ana ", 10, "player-key-1"));

        Assert.True(result.IsSuccess);
        var player = Assert.Single(fixture.Writes.Added);
        Assert.Equal("Ana", player.Name);
        Assert.True(player.IsActive);
        Assert.Equal(player.Id, result.Value.PlayerId);
        Assert.Single(fixture.Store.Staged);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task MissingTeam_ReturnsNotFoundWithoutWriting()
    {
        var fixture = new Fixture(teamExists: false);

        var result = await fixture.Handler.HandleAsync(
            new(fixture.TeamId, "Ana", 10, "player-key-1"));

        Assert.Equal(PlayerMutationErrors.TeamNotFound, result.Error);
        Assert.Empty(fixture.Writes.Added);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task MissingIdempotencyKey_ReturnsValidation()
    {
        var fixture = new Fixture(teamExists: true);
        var result = await fixture.Handler.HandleAsync(new(fixture.TeamId, "Ana", 10, null));
        Assert.Equal(IdempotencyErrors.KeyRequired, result.Error);
    }

    [Fact]
    public async Task SameKeyAndRequest_ReplaysOriginalResponse()
    {
        var fixture = new Fixture(teamExists: true);
        var first = await fixture.Handler.HandleAsync(
            new(fixture.TeamId, "Ana", 10, "player-key-1"));
        fixture.Store.PublishStaged();

        var replay = await fixture.Handler.HandleAsync(
            new(fixture.TeamId, "Ana", 10, "player-key-1"));

        Assert.True(replay.Value.IsReplay);
        Assert.Equal(first.Value.ResponseBody, replay.Value.ResponseBody);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task SameKeyAndDifferentRequest_ReturnsConflict()
    {
        var fixture = new Fixture(teamExists: true);
        await fixture.Handler.HandleAsync(new(fixture.TeamId, "Ana", 10, "player-key-1"));
        fixture.Store.PublishStaged();

        var result = await fixture.Handler.HandleAsync(
            new(fixture.TeamId, "Bea", 11, "player-key-1"));

        Assert.Equal(IdempotencyErrors.KeyReused, result.Error);
        Assert.Single(fixture.Writes.Added);
    }

    [Fact]
    public async Task ConcurrentIdempotencyKeyLoss_ReplaysCommittedWinner()
    {
        var fixture = new Fixture(
            teamExists: true,
            Result<int>.Failure(PersistenceErrors.ConstraintViolation));
        fixture.Store.PublishOnSecondFind = true;

        var result = await fixture.Handler.HandleAsync(
            new(fixture.TeamId, "Ana", 10, "player-key-1"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
    }

    private sealed class Fixture
    {
        public Guid TeamId { get; } = Guid.NewGuid();
        public TeamReadRepository Teams { get; }
        public PlayerWriteRepository Writes { get; } = new();
        public StubUnitOfWork UnitOfWork { get; }
        public StubStore Store { get; } = new();
        public CreatePlayerCommandHandler Handler { get; }

        public Fixture(bool teamExists, Result<int>? commit = null)
        {
            Teams = new(teamExists ? new(TeamId, "Argentina", "ARG") : null);
            UnitOfWork = new(commit ?? Result<int>.Success(2));
            Handler = new(Teams, Writes, UnitOfWork, Store);
        }
    }

    private sealed class TeamReadRepository(TeamListItem? team) : ITeamReadRepository
    {
        public Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(team?.Id == id ? team : null);
        }
        public Task<PagedResult<TeamListItem>> GetPageAsync(
            TeamPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class PlayerWriteRepository : IWriteRepository<Player>
    {
        public List<Player> Added { get; } = [];
        public void Add(Player entity) => Added.Add(entity);
        public void Update(Player entity) => throw new NotSupportedException();
        public void Remove(Player entity) => throw new NotSupportedException();
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
