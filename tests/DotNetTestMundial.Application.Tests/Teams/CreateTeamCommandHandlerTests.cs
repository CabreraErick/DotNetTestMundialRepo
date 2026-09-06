using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Tests.Teams;

public sealed class CreateTeamCommandHandlerTests
{
    [Fact]
    public async Task ValidCommand_AddsNormalizedTeamAndCommitsOnce()
    {
        var repository = new RecordingRepository<Team>();
        var unitOfWork = new StubUnitOfWork(Result<int>.Success(1));
        var handler = new CreateTeamCommandHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(new CreateTeamCommand("  Águilas  ", " ag "));

        Assert.True(result.IsSuccess);
        var team = Assert.Single(repository.Added);
        Assert.Equal(result.Value, team.Id);
        Assert.Equal("Águilas", team.Name);
        Assert.Equal("AG", team.ShortName);
        Assert.Equal(1, unitOfWork.CommitCalls);
    }

    [Theory]
    [InlineData(null, "AG")]
    [InlineData(" ", "AG")]
    [InlineData("Águilas", null)]
    [InlineData("Águilas", " ")]
    public async Task InvalidCommand_ReturnsDomainErrorWithoutWriting(string? name, string? shortName)
    {
        var repository = new RecordingRepository<Team>();
        var unitOfWork = new StubUnitOfWork(Result<int>.Success(1));
        var handler = new CreateTeamCommandHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(new CreateTeamCommand(name, shortName));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(repository.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task PersistenceConflict_ReturnsOriginalErrorAfterOneCommitAttempt()
    {
        var repository = new RecordingRepository<Team>();
        var unitOfWork = new StubUnitOfWork(Result<int>.Failure(PersistenceErrors.ConstraintViolation));
        var handler = new CreateTeamCommandHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(new CreateTeamCommand("Águilas", "AG"));

        Assert.True(result.IsFailure);
        Assert.Equal(PersistenceErrors.ConstraintViolation, result.Error);
        Assert.Single(repository.Added);
        Assert.Equal(1, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task Cancellation_IsForwardedToUnitOfWork()
    {
        var repository = new RecordingRepository<Team>();
        var unitOfWork = new StubUnitOfWork(Result<int>.Success(1));
        var handler = new CreateTeamCommandHandler(repository, unitOfWork);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(new CreateTeamCommand("Águilas", "AG"), cancellation.Token));

        Assert.Equal(1, unitOfWork.CommitCalls);
    }

    private sealed class RecordingRepository<TEntity> : IWriteRepository<TEntity> where TEntity : Entity
    {
        public List<TEntity> Added { get; } = [];
        public void Add(TEntity entity) => Added.Add(entity);
        public void Update(TEntity entity) => throw new NotSupportedException();
        public void Remove(TEntity entity) => throw new NotSupportedException();
    }

    private sealed class StubUnitOfWork(Result<int> result) : IUnitOfWork
    {
        public int CommitCalls { get; private set; }

        public Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }

        public void Rollback() { }
    }
}
