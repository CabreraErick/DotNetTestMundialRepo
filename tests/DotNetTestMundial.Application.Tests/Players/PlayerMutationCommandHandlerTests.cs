// Responsabilidad del archivo: Verifica PUT, PATCH y baja lógica DELETE de jugadores.
// Relación en el sistema: Usa dobles de Dapper, EF y Unit of Work para aislar la orquestación de Application.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Players.GetPlayers;
using DotNetTestMundial.Application.Players.Mutations;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Tests.Players;

public sealed class PlayerMutationCommandHandlerTests
{
    [Fact]
    public async Task Update_ExistingPlayer_StagesNormalizedChangeAndCommits()
    {
        var fixture = new Fixture(ActivePlayer());

        var result = await fixture.Update.HandleAsync(new(fixture.PlayerId, " Ana Maria ", 20));

        Assert.True(result.IsSuccess);
        var updated = Assert.Single(fixture.Writes.Updated);
        Assert.Equal("Ana Maria", updated.Name);
        Assert.Equal(20, updated.JerseyNumber);
        Assert.Equal(fixture.TeamId, updated.TeamId);
        Assert.True(updated.IsActive);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Update_InvalidInput_DoesNotWrite()
    {
        var fixture = new Fixture(ActivePlayer());

        var result = await fixture.Update.HandleAsync(new(fixture.PlayerId, " ", 10));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(fixture.Writes.Updated);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Patch_OmittedName_PreservesIt()
    {
        var fixture = new Fixture(ActivePlayer());

        var result = await fixture.Patch.HandleAsync(new(fixture.PlayerId, null, 11));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana", result.Value.Name);
        Assert.Equal(11, result.Value.JerseyNumber);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Patch_EmptyRequest_StopsBeforeRead()
    {
        var fixture = new Fixture(ActivePlayer());

        var result = await fixture.Patch.HandleAsync(new(fixture.PlayerId, null, null));

        Assert.Equal(PlayerMutationErrors.PatchEmpty, result.Error);
        Assert.Equal(0, fixture.Reads.FindCalls);
    }

    [Fact]
    public async Task Delete_ActivePlayer_DeactivatesAndCommits()
    {
        var fixture = new Fixture(ActivePlayer());

        var result = await fixture.Delete.HandleAsync(new(fixture.PlayerId));

        Assert.True(result.IsSuccess);
        Assert.False(Assert.Single(fixture.Writes.Updated).IsActive);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Delete_InactivePlayer_IsIdempotentWithoutAnotherCommit()
    {
        var player = ActivePlayer() with { IsActive = false };
        var fixture = new Fixture(player);

        var result = await fixture.Delete.HandleAsync(new(player.Id));

        Assert.True(result.IsSuccess);
        Assert.Empty(fixture.Writes.Updated);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task MissingPlayer_ReturnsNotFound()
    {
        var fixture = new Fixture(null);

        var result = await fixture.Update.HandleAsync(new(Guid.NewGuid(), "Ana", 10));

        Assert.Equal(PlayerMutationErrors.NotFound, result.Error);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task CommitConflict_IsPropagated()
    {
        var fixture = new Fixture(
            ActivePlayer(), Result<int>.Failure(PersistenceErrors.ConcurrentChange));

        var result = await fixture.Delete.HandleAsync(new(fixture.PlayerId));

        Assert.Equal(PersistenceErrors.ConcurrentChange, result.Error);
    }

    private static PlayerListItem ActivePlayer() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Ana", 10, true);

    private sealed class Fixture
    {
        public StubReadRepository Reads { get; }
        public RecordingWriteRepository Writes { get; } = new();
        public StubUnitOfWork UnitOfWork { get; }
        public UpdatePlayerCommandHandler Update { get; }
        public PatchPlayerCommandHandler Patch { get; }
        public DeletePlayerCommandHandler Delete { get; }
        public Guid PlayerId => Reads.Response?.Id ?? Guid.Empty;
        public Guid TeamId => Reads.Response?.TeamId ?? Guid.Empty;

        public Fixture(PlayerListItem? player, Result<int>? commit = null)
        {
            Reads = new(player);
            UnitOfWork = new(commit ?? Result<int>.Success(1));
            Update = new(Reads, Writes, UnitOfWork);
            Patch = new(Reads, Writes, UnitOfWork);
            Delete = new(Reads, Writes, UnitOfWork);
        }
    }

    private sealed class StubReadRepository(PlayerListItem? response) : IPlayerReadRepository
    {
        public PlayerListItem? Response { get; } = response;
        public int FindCalls { get; private set; }
        public Task<PlayerListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            FindCalls++;
            return Task.FromResult(Response?.Id == id ? Response : null);
        }
        public Task<PagedResult<PlayerListItem>> GetPageAsync(
            PlayerPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingWriteRepository : IWriteRepository<Player>
    {
        public List<Player> Updated { get; } = [];
        public void Add(Player entity) => throw new NotSupportedException();
        public void Update(Player entity) => Updated.Add(entity);
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
}
