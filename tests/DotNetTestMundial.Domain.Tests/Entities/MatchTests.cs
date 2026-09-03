using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Enums;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class MatchTests
{
    [Fact]
    public void Create_ShouldCreateScheduledMatch()
    {
        var match = Match.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1));

        Assert.Equal(
            MatchStatus.Scheduled,
            match.Status);

        Assert.Null(match.HomeScore);
        Assert.Null(match.AwayScore);
    }

    [Fact]
    public void Create_WithSameTeams_ShouldThrow()
    {
        var teamId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            Match.Create(
                teamId,
                teamId,
                DateTime.UtcNow));
    }

    [Fact]
    public void RegisterResult_ShouldMarkMatchAsPlayed()
    {
        var match = Match.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        match.RegisterResult(3, 1);

        Assert.Equal(
            MatchStatus.Played,
            match.Status);

        Assert.Equal(3, match.HomeScore);
        Assert.Equal(1, match.AwayScore);
    }

    [Fact]
    public void RegisterResult_ShouldRaiseDomainEvent()
    {
        var match = Match.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        match.RegisterResult(2, 1);

        Assert.Contains(
            match.DomainEvents,
            domainEvent =>
                domainEvent is MatchResultRegisteredEvent);
    }

    [Fact]
    public void RegisterResult_WithNegativeScore_ShouldThrow()
    {
        var match = Match.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            match.RegisterResult(-1, 0));
    }

    [Fact]
    public void Cancel_ShouldMarkMatchAsCancelled()
    {
        var match = Match.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        match.Cancel();

        Assert.Equal(
            MatchStatus.Cancelled,
            match.Status);
    }
}