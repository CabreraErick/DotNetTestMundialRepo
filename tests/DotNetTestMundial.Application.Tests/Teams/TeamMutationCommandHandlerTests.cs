// Responsabilidad del archivo: Verifica los Commands PUT, PATCH y DELETE de equipos.
// Relación en el sistema: Aísla Application con dobles de Dapper, EF y Unit of Work para comprobar su orquestación.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Application.Teams.Mutations;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Tests.Teams;

public sealed class TeamMutationCommandHandlerTests
{
    [Fact]
    public async Task Update_ExistingTeam_NormalizesStagesAndCommits()
    {
        var fixture = new Fixture(new(Guid.NewGuid(), "Argentina", "ARG"));

        var result = await fixture.Update.HandleAsync(
            new(fixture.TeamId, "  Aguilas  ", " ag "));

        Assert.True(result.IsSuccess);
        var updated = Assert.Single(fixture.Writes.Updated);
        Assert.Equal("Aguilas", updated.Name);
        Assert.Equal("AG", updated.ShortName);
        Assert.Equal(updated.Id, result.Value.Id);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Update_InvalidDomainInput_DoesNotStageOrCommit()
    {
        var fixture = new Fixture(new(Guid.NewGuid(), "Argentina", "ARG"));

        var result = await fixture.Update.HandleAsync(new(fixture.TeamId, " ", "ARG"));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(fixture.Writes.Updated);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Update_MissingTeam_ReturnsNotFound()
    {
        var fixture = new Fixture(null);

        var result = await fixture.Update.HandleAsync(new(Guid.NewGuid(), "Chile", "CHI"));

        Assert.Equal(TeamMutationErrors.NotFound, result.Error);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Patch_OmittedField_PreservesPersistedValue()
    {
        var fixture = new Fixture(new(Guid.NewGuid(), "Argentina", "ARG"));

        var result = await fixture.Patch.HandleAsync(new(fixture.TeamId, null, " alb "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Argentina", result.Value.Name);
        Assert.Equal("ALB", result.Value.ShortName);
        Assert.Single(fixture.Writes.Updated);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Patch_EmptyRequest_ReturnsValidationBeforeReading()
    {
        var fixture = new Fixture(new(Guid.NewGuid(), "Argentina", "ARG"));

        var result = await fixture.Patch.HandleAsync(new(fixture.TeamId, null, null));

        Assert.Equal(TeamMutationErrors.PatchEmpty, result.Error);
        Assert.Equal(0, fixture.Reads.FindCalls);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Delete_ExistingTeam_StagesRemovalAndCommits()
    {
        var fixture = new Fixture(new(Guid.NewGuid(), "Argentina", "ARG"));

        var result = await fixture.Delete.HandleAsync(new(fixture.TeamId));

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.TeamId, result.Value);
        Assert.Single(fixture.Writes.Removed);
        Assert.Equal(1, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Delete_MissingTeam_ReturnsNotFound()
    {
        var fixture = new Fixture(null);

        var result = await fixture.Delete.HandleAsync(new(Guid.NewGuid()));

        Assert.Equal(TeamMutationErrors.NotFound, result.Error);
        Assert.Empty(fixture.Writes.Removed);
        Assert.Equal(0, fixture.UnitOfWork.CommitCalls);
    }

    [Fact]
    public async Task CommitFailure_IsReturnedByCommand()
    {
        var fixture = new Fixture(
            new(Guid.NewGuid(), "Argentina", "ARG"),
            Result<int>.Failure(PersistenceErrors.ConstraintViolation));

        var result = await fixture.Delete.HandleAsync(new(fixture.TeamId));

        Assert.Equal(PersistenceErrors.ConstraintViolation, result.Error);
    }

    private sealed class Fixture
    {
        public StubReadRepository Reads { get; }
        public RecordingWriteRepository Writes { get; } = new();
        public StubUnitOfWork UnitOfWork { get; }
        public UpdateTeamCommandHandler Update { get; }
        public PatchTeamCommandHandler Patch { get; }
        public DeleteTeamCommandHandler Delete { get; }
        public Guid TeamId => Reads.Response?.Id ?? Guid.NewGuid();

        public Fixture(TeamListItem? team, Result<int>? commit = null)
        {
            Reads = new(team);
            UnitOfWork = new(commit ?? Result<int>.Success(1));
            var identityValidator = new TeamIdentityValidator(Reads);
            Update = new(Reads, identityValidator, Writes, UnitOfWork);
            Patch = new(Reads, identityValidator, Writes, UnitOfWork);
            Delete = new(Reads, Writes, UnitOfWork);
        }
    }

    private sealed class StubReadRepository(TeamListItem? response) : ITeamReadRepository
    {
        public TeamListItem? Response { get; } = response;
        public int FindCalls { get; private set; }

        public Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            FindCalls++;
            return Task.FromResult(Response?.Id == id ? Response : null);
        }

        public Task<TeamIdentityConflict> FindIdentityConflictAsync(
            string name, string shortName, Guid? excludingId = null,
            CancellationToken token = default) =>
            Task.FromResult(new TeamIdentityConflict(false, false));

        public Task<PagedResult<TeamListItem>> GetPageAsync(
            TeamPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingWriteRepository : IWriteRepository<Team>
    {
        public List<Team> Updated { get; } = [];
        public List<Team> Removed { get; } = [];
        public void Add(Team entity) => throw new NotSupportedException();
        public void Update(Team entity) => Updated.Add(entity);
        public void Remove(Team entity) => Removed.Add(entity);
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
