// Responsabilidad del archivo: Verifica el Command de creación e idempotencia de equipos.
// Relación en el sistema: Usa dobles de los puertos Application para aislar reglas de orquestación.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Application.Teams.Mutations;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Tests.Teams;

public sealed class CreateTeamCommandHandlerTests
{
    [Fact]
    public async Task ValidCommand_StagesTeamAndResponseThenCommits()
    {
        var f = new Fixture();
        var result = await f.Handler.HandleAsync(new("  Águilas  ", " ag ", "key-1"));
        Assert.True(result.IsSuccess);
        var team = Assert.Single(f.Repository.Added);
        Assert.Equal(team.Id, result.Value.TeamId);
        Assert.Equal("Águilas", team.Name);
        Assert.Equal("AG", team.ShortName);
        Assert.False(result.Value.IsReplay);
        Assert.Single(f.Store.Staged);
        Assert.Equal(1, f.UnitOfWork.CommitCalls);
    }

    [Theory]
    [InlineData(null, "AG")]
    [InlineData(" ", "AG")]
    [InlineData("Águilas", null)]
    [InlineData("Águilas", " ")]
    public async Task InvalidBusinessInput_DoesNotWrite(string? name, string? shortName)
    {
        var f = new Fixture();
        var result = await f.Handler.HandleAsync(new(name, shortName, "key-1"));
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(f.Repository.Added);
        Assert.Equal(0, f.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task MissingKey_DoesNotWrite()
    {
        var f = new Fixture();
        var result = await f.Handler.HandleAsync(new("Águilas", "AG", null));
        Assert.Equal(IdempotencyErrors.KeyRequired, result.Error);
        Assert.Empty(f.Repository.Added);
    }

    [Fact]
    public async Task SameKeyAndRequest_ReplaysOriginalResponse()
    {
        var f = new Fixture();
        var first = await f.Handler.HandleAsync(new("Águilas", "AG", "key-1"));
        f.Store.PublishStaged();
        var replay = await f.Handler.HandleAsync(new("Águilas", "AG", "key-1"));
        Assert.True(replay.Value.IsReplay);
        Assert.Equal(first.Value.ResponseBody, replay.Value.ResponseBody);
        Assert.Equal(1, f.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task SameKeyAndDifferentRequest_ReturnsConflict()
    {
        var f = new Fixture();
        await f.Handler.HandleAsync(new("Águilas", "AG", "key-1"));
        f.Store.PublishStaged();
        var result = await f.Handler.HandleAsync(new("Tigres", "TG", "key-1"));
        Assert.Equal(IdempotencyErrors.KeyReused, result.Error);
        Assert.Equal(1, f.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task ConcurrentKeyLoss_ReplaysWinningResponse()
    {
        var f = new Fixture(Result<int>.Failure(PersistenceErrors.ConstraintViolation));
        f.Store.PublishOnSecondFind = true;
        var result = await f.Handler.HandleAsync(new("Águilas", "AG", "key-1"));
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
    }

    [Fact]
    public async Task DuplicateName_ReturnsTypedConflictWithoutWriting()
    {
        var f = new Fixture(new TeamIdentityConflict(true, false));

        var result = await f.Handler.HandleAsync(new("Argentina", "NEW", "key-1"));

        Assert.Equal(TeamMutationErrors.NameAlreadyExists, result.Error);
        Assert.Empty(f.Repository.Added);
        Assert.Empty(f.Store.Staged);
        Assert.Equal(0, f.UnitOfWork.CommitCalls);
    }

    private sealed class Fixture
    {
        public RecordingRepository Repository { get; } = new();
        public StubUnitOfWork UnitOfWork { get; }
        public StubStore Store { get; } = new();
        public CreateTeamCommandHandler Handler { get; }
        public Fixture(Result<int>? commit = null) : this(new(false, false), commit) { }
        public Fixture(TeamIdentityConflict conflict, Result<int>? commit = null)
        {
            UnitOfWork = new(commit ?? Result<int>.Success(2));
            Handler = new(new TeamIdentityValidator(new IdentityReadRepository(conflict)), Repository, UnitOfWork, Store);
        }
    }

    private sealed class IdentityReadRepository(TeamIdentityConflict conflict) : ITeamReadRepository
    {
        public Task<TeamIdentityConflict> FindIdentityConflictAsync(
            string name, string shortName, Guid? excludingId = null,
            CancellationToken token = default) =>
            Task.FromResult(conflict);
        public Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<PagedResult<TeamListItem>> GetPageAsync(
            TeamPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingRepository : IWriteRepository<Team>
    {
        public List<Team> Added { get; } = [];
        public void Add(Team entity) => Added.Add(entity);
        public void Update(Team entity) => throw new NotSupportedException();
        public void Remove(Team entity) => throw new NotSupportedException();
    }

    private sealed class StubUnitOfWork(Result<int> result) : IUnitOfWork
    {
        public int CommitCalls { get; private set; }
        public Task<Result<int>> CommitAsync(CancellationToken token = default)
        { CommitCalls++; token.ThrowIfCancellationRequested(); return Task.FromResult(result); }
        public void Rollback() { }
    }

    private sealed class StubStore : IIdempotencyStore
    {
        private StoredIdempotentResponse? _visible;
        private int _finds;
        public bool PublishOnSecondFind { get; set; }
        public List<StoredIdempotentResponse> Staged { get; } = [];
        public void Stage(StoredIdempotentResponse response) => Staged.Add(response);
        public void PublishStaged() => _visible = Staged.Single();
        public Task<StoredIdempotentResponse?> FindAsync(string operation, string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (PublishOnSecondFind && ++_finds == 2) PublishStaged();
            return Task.FromResult(_visible);
        }
    }
}
