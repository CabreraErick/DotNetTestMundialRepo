using System.Data.Common;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Enums;
using DotNetTestMundial.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DotNetTestMundial.Infrastructure.Tests;

public class UnitOfWorkIntegrationTests
{
    [Fact]
    public async Task Commit_PersistsMultipleRepositoriesOnlyAfterExplicitConfirmation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = Team.Create("Local", "LOC").Value;
        var player = Player.Create(team.Id, "Ana", 10).Value;
        new WriteRepository<Team>(context).Add(team);
        new WriteRepository<Player>(context).Add(player);
        await using var reader = database.CreateContext();
        Assert.Equal(0, await reader.Set<Team>().CountAsync());
        Assert.Equal(0, await reader.Set<Player>().CountAsync());

        var result = await SqliteTestDatabase.CreateUnitOfWork(context).CommitAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        var loaded = await reader.Set<Team>().Include(t => t.Players).SingleAsync();
        Assert.Equal(team.Id, loaded.Id);
        Assert.Equal("Local", loaded.Name);
        Assert.Equal("LOC", loaded.ShortName);
        var loadedPlayer = Assert.Single(loaded.Players);
        Assert.Equal(player.Id, loadedPlayer.Id);
        Assert.Equal(team.Id, loadedPlayer.TeamId);
        Assert.Equal(10, loadedPlayer.JerseyNumber);
        Assert.True(loadedPlayer.IsActive);
        Assert.Empty(loaded.DomainEvents);
        Assert.Single(team.DomainEvents); // persistence does not publish or discard events yet
    }

    [Fact]
    public async Task Commit_SavesResultAndNewGoalsTogetherAndReloadsPrivateCollections()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var home = Team.Create("Local", "LOC").Value;
        var away = Team.Create("Visitante", "VIS").Value;
        var player = Player.Create(home.Id, "Ana", 10).Value;
        var match = Match.Create(home.Id, away.Id, new DateTime(2026, 9, 2, 12, 0, 0)).Value;
        var teams = new WriteRepository<Team>(context);
        teams.Add(home);
        teams.Add(away);
        new WriteRepository<Player>(context).Add(player);
        new WriteRepository<Match>(context).Add(match);
        Assert.True((await SqliteTestDatabase.CreateUnitOfWork(context).CommitAsync()).IsSuccess);
        context.ChangeTracker.Clear();

        var goal = Goal.Create(match.Id, player, 10).Value;
        Assert.True(match.AddGoal(goal).IsSuccess);
        Assert.True(match.RegisterResult(1, 0).IsSuccess);
        new WriteRepository<Goal>(context).Add(goal);
        new WriteRepository<Match>(context).Update(match);
        Assert.True((await SqliteTestDatabase.CreateUnitOfWork(context).CommitAsync()).IsSuccess);

        await using var reader = database.CreateContext();
        var loaded = await reader.Set<Match>().Include(m => m.Goals).SingleAsync();
        Assert.Equal(MatchStatus.Played, loaded.Status);
        Assert.Equal(1, loaded.HomeScore);
        Assert.Equal(0, loaded.AwayScore);
        Assert.Equal(match.ScheduledAt, loaded.ScheduledAt);
        var loadedGoal = Assert.Single(loaded.Goals);
        Assert.Equal(goal.Id, loadedGoal.Id);
        Assert.Equal(match.Id, loadedGoal.MatchId);
        Assert.Equal(player.Id, loadedGoal.PlayerId);
        Assert.Equal(home.Id, loadedGoal.TeamId);
        Assert.Equal(10, loadedGoal.Minute);
        Assert.Empty(loaded.DomainEvents);
    }

    [Fact]
    public async Task ConstraintFailure_RollsBackOtherWritesAndClearsPendingState()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        new WriteRepository<Team>(context).Add(Team.Create("Valid", "VAL").Value);
        new WriteRepository<Player>(context).Add(Player.Create(Guid.NewGuid(), "Missing team", 9).Value);
        var unitOfWork = SqliteTestDatabase.CreateUnitOfWork(context);

        var result = await unitOfWork.CommitAsync();

        Assert.Equal(PersistenceErrors.ConstraintViolation, result.Error);
        Assert.Empty(context.ChangeTracker.Entries());
        await using var reader = database.CreateContext();
        Assert.Equal(0, await reader.Set<Team>().CountAsync());
        Assert.Equal(0, await reader.Set<Player>().CountAsync());
        Assert.Equal(0, (await unitOfWork.CommitAsync()).Value);
    }

    [Fact]
    public async Task CommitFailure_AfterSaveRollsBackRowsAlreadyWritten()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var interceptor = new CommitProbe(fail: true);
        await using var context = database.CreateContext(interceptor);
        new WriteRepository<Team>(context).Add(Team.Create("Local", "LOC").Value);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SqliteTestDatabase.CreateUnitOfWork(context).CommitAsync());

        Assert.Equal(1, interceptor.RowsBeforeCommit);
        Assert.True(interceptor.StatesStillPending);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Null(context.Database.CurrentTransaction);
        await using var reader = database.CreateContext();
        Assert.Equal(0, await reader.Set<Team>().CountAsync());
    }

    [Fact]
    public async Task Commit_AcceptsEntityStatesOnlyAfterSuccessfulDatabaseCommit()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var interceptor = new CommitProbe(fail: false);
        await using var context = database.CreateContext(interceptor);
        var team = Team.Create("Local", "LOC").Value;
        new WriteRepository<Team>(context).Add(team);
        Assert.True((await SqliteTestDatabase.CreateUnitOfWork(context).CommitAsync()).IsSuccess);
        Assert.True(interceptor.StatesStillPending);
        Assert.Equal(EntityState.Unchanged, context.Entry(team).State);
    }

    [Fact]
    public async Task CancellationAfterSave_StillRollsBackWithUncancelledCleanupToken()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CommitProbe(fail: false, cancellation);
        await using var context = database.CreateContext(interceptor);
        new WriteRepository<Team>(context).Add(Team.Create("Local", "LOC").Value);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            SqliteTestDatabase.CreateUnitOfWork(context).CommitAsync(cancellation.Token));
        Assert.Equal(1, interceptor.RowsBeforeCommit);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Null(context.Database.CurrentTransaction);
        await using var reader = database.CreateContext();
        Assert.Equal(0, await reader.Set<Team>().CountAsync());
    }

    [Fact]
    public async Task Rollback_DiscardedChangesCannotLeakIntoNextCommit()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var repository = new WriteRepository<Team>(context);
        var unitOfWork = SqliteTestDatabase.CreateUnitOfWork(context);
        repository.Add(Team.Create("Discarded", "DIS").Value);
        unitOfWork.Rollback();
        repository.Add(Team.Create("Saved", "SAV").Value);
        Assert.True((await unitOfWork.CommitAsync()).IsSuccess);
        await using var reader = database.CreateContext();
        Assert.Equal("Saved", (await reader.Set<Team>().SingleAsync()).Name);
    }

    [Fact]
    public async Task SecondCommitWithoutChanges_DoesNotDuplicateRows()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        new WriteRepository<Team>(context).Add(Team.Create("Local", "LOC").Value);
        var unitOfWork = SqliteTestDatabase.CreateUnitOfWork(context);
        Assert.Equal(1, (await unitOfWork.CommitAsync()).Value);
        Assert.Equal(0, (await unitOfWork.CommitAsync()).Value);
        await using var reader = database.CreateContext();
        Assert.Equal(1, await reader.Set<Team>().CountAsync());
    }

    [Fact]
    public async Task RemovingReferencedTeam_IsRejectedWithoutLosingPlayers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = Team.Create("Local", "LOC").Value;
        new WriteRepository<Team>(context).Add(team);
        new WriteRepository<Player>(context).Add(Player.Create(team.Id, "Ana", 10).Value);
        var unitOfWork = SqliteTestDatabase.CreateUnitOfWork(context);
        Assert.True((await unitOfWork.CommitAsync()).IsSuccess);
        context.ChangeTracker.Clear();
        new WriteRepository<Team>(context).Remove(team);
        Assert.Equal(PersistenceErrors.ConstraintViolation, (await unitOfWork.CommitAsync()).Error);
        await using var reader = database.CreateContext();
        Assert.Equal(1, await reader.Set<Team>().CountAsync());
        Assert.Equal(1, await reader.Set<Player>().CountAsync());
    }

    [Fact]
    public async Task UpdatingRemovedRecord_ReturnsConflictAndDiscardsOtherPendingChanges()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = Team.Create("Local", "LOC").Value;
        var repository = new WriteRepository<Team>(context);
        repository.Add(team);
        var unitOfWork = SqliteTestDatabase.CreateUnitOfWork(context);
        await unitOfWork.CommitAsync();
        context.ChangeTracker.Clear();
        await using (var deleting = database.CreateContext())
        {
            new WriteRepository<Team>(deleting).Remove(team);
            Assert.True((await SqliteTestDatabase.CreateUnitOfWork(deleting).CommitAsync()).IsSuccess);
        }
        team.Update("Changed", "CHG");
        repository.Update(team);
        repository.Add(Team.Create("Must rollback", "ROLL").Value);
        Assert.Equal(PersistenceErrors.ConcurrentChange, (await unitOfWork.CommitAsync()).Error);
        Assert.Empty(context.ChangeTracker.Entries());
        await using var reader = database.CreateContext();
        Assert.Equal(0, await reader.Set<Team>().CountAsync());
    }

    private sealed class CommitProbe(bool fail, CancellationTokenSource? cancellation = null) : DbTransactionInterceptor
    {
        public int RowsBeforeCommit { get; private set; }
        public bool StatesStillPending { get; private set; }

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction, TransactionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            RowsBeforeCommit = await eventData.Context!.Set<Team>().CountAsync(cancellationToken);
            StatesStillPending = eventData.Context.ChangeTracker.Entries<Team>().Any(e => e.State == EntityState.Added);
            if (fail)
                throw new InvalidOperationException("Injected failure after SaveChanges and before commit.");
            if (cancellation is not null)
            {
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return result;
        }
    }
}
