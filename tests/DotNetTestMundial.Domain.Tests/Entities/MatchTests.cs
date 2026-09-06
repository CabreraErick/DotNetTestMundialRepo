using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Enums;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class MatchTests
{
    private static Match CreateMatch() =>
        Match.Create(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 1, 15, 0, 0, DateTimeKind.Utc)).Value;

    private static Goal CreateGoal(Match match, Guid teamId, int minute = 10) =>
        Goal.Create(match.Id, Player.Create(teamId, "Jugador", 9).Value, minute).Value;

    [Fact]
    public void Create_StartsScheduledWithoutScoreGoalsOrEvents()
    {
        var match = CreateMatch();
        Assert.Equal(MatchStatus.Scheduled, match.Status);
        Assert.Null(match.HomeScore);
        Assert.Null(match.AwayScore);
        Assert.Empty(match.Goals);
        Assert.Empty(match.DomainEvents);
    }

    [Fact]
    public void Create_RejectsMissingOrIdenticalTeams()
    {
        var id = Guid.NewGuid();
        var date = DateTime.UtcNow;
        Assert.Equal(DomainErrors.HomeTeamRequired, Match.Create(Guid.Empty, id, date).Error);
        Assert.Equal(DomainErrors.AwayTeamRequired, Match.Create(id, Guid.Empty, date).Error);
        Assert.Equal(DomainErrors.SameTeams, Match.Create(id, id, date).Error);
    }

    [Fact]
    public void AddGoal_RejectsNullAndGoalFromAnotherMatch()
    {
        var match = CreateMatch();
        var player = Player.Create(match.HomeTeamId, "Jugador", 9).Value;
        var wrongMatchGoal = Goal.Create(Guid.NewGuid(), player, 10).Value;
        Assert.Equal(DomainErrors.GoalRequired, match.AddGoal(null).Error);
        Assert.Equal(DomainErrors.GoalMatchMismatch, match.AddGoal(wrongMatchGoal).Error);
        Assert.Empty(match.Goals);
    }

    [Fact]
    public void AddGoal_RejectsScorerFromThirdTeamWithoutChangingMatch()
    {
        var match = CreateMatch();
        var result = match.AddGoal(CreateGoal(match, Guid.NewGuid()));
        Assert.Equal(DomainErrors.ScorerTeamMismatch, result.Error);
        Assert.Empty(match.Goals);
        Assert.Empty(match.DomainEvents);
    }

    [Fact]
    public void AddGoal_RejectsDuplicateIdentityButAllowsDistinctGoalsInSameMinute()
    {
        var match = CreateMatch();
        var player = Player.Create(match.HomeTeamId, "Jugador", 9).Value;
        var first = Goal.Create(match.Id, player, 10).Value;
        Assert.True(match.AddGoal(first).IsSuccess);
        Assert.Equal(DomainErrors.DuplicateGoal, match.AddGoal(first).Error);
        Assert.Single(match.Goals);
        Assert.True(match.AddGoal(Goal.Create(match.Id, player, 10).Value).IsSuccess);
        Assert.Equal(2, match.Goals.Count);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void RegisterResult_RejectsNegativeScoreWithoutMutation(int homeScore, int awayScore)
    {
        var match = CreateMatch();
        Assert.Equal(DomainErrors.NegativeScore, match.RegisterResult(homeScore, awayScore).Error);
        AssertUnfinalized(match);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(2, 0)]
    public void RegisterResult_RejectsInconsistentScoreByTeam(int homeScore, int awayScore)
    {
        var match = CreateMatch();
        match.AddGoal(CreateGoal(match, match.HomeTeamId));
        Assert.Equal(DomainErrors.ScoreMismatch, match.RegisterResult(homeScore, awayScore).Error);
        AssertUnfinalized(match);
        Assert.Single(match.Goals);
    }

    [Fact]
    public void RegisterResult_FailedAttemptCanBeCorrectedBeforeFinalization()
    {
        var match = CreateMatch();
        Assert.True(match.RegisterResult(1, 0).IsFailure);
        Assert.True(match.AddGoal(CreateGoal(match, match.HomeTeamId)).IsSuccess);
        Assert.True(match.RegisterResult(1, 0).IsSuccess);
        Assert.Single(match.DomainEvents);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 1)]
    public void RegisterResult_FinalizesConsistentScoreAndRaisesOneEvent(int homeScore, int awayScore)
    {
        var match = CreateMatch();
        for (var i = 0; i < homeScore; i++)
            Assert.True(match.AddGoal(CreateGoal(match, match.HomeTeamId, i + 1)).IsSuccess);
        for (var i = 0; i < awayScore; i++)
            Assert.True(match.AddGoal(CreateGoal(match, match.AwayTeamId, i + 1)).IsSuccess);

        var before = DateTime.UtcNow;
        Assert.True(match.RegisterResult(homeScore, awayScore).IsSuccess);

        Assert.Equal(MatchStatus.Played, match.Status);
        Assert.Equal(homeScore, match.HomeScore);
        Assert.Equal(awayScore, match.AwayScore);
        var domainEvent = Assert.IsType<MatchResultRegisteredEvent>(Assert.Single(match.DomainEvents));
        Assert.Equal(match.Id, domainEvent.MatchId);
        Assert.Equal(homeScore, domainEvent.HomeScore);
        Assert.Equal(awayScore, domainEvent.AwayScore);
        Assert.Equal(DateTimeKind.Utc, domainEvent.OccurredAt.Kind);
        Assert.InRange(domainEvent.OccurredAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void PlayedMatch_RejectsNewGoalsResultsAndCancellationWithoutNewEvents()
    {
        var match = CreateMatch();
        match.RegisterResult(0, 0);
        Assert.Equal(DomainErrors.MatchNotScheduled, match.AddGoal(CreateGoal(match, match.HomeTeamId)).Error);
        Assert.Equal(DomainErrors.MatchNotScheduled, match.RegisterResult(0, 0).Error);
        Assert.Equal(DomainErrors.MatchNotScheduled, match.Cancel().Error);
        Assert.Equal(MatchStatus.Played, match.Status);
        Assert.Equal(0, match.HomeScore);
        Assert.Empty(match.Goals);
        Assert.Single(match.DomainEvents);
    }

    [Fact]
    public void CancelledMatch_RejectsGoalsAndResults()
    {
        var match = CreateMatch();
        Assert.True(match.Cancel().IsSuccess);
        Assert.Equal(DomainErrors.MatchNotScheduled, match.RegisterResult(0, 0).Error);
        Assert.Equal(DomainErrors.MatchNotScheduled, match.AddGoal(CreateGoal(match, match.HomeTeamId)).Error);
        Assert.Equal(MatchStatus.Cancelled, match.Status);
        Assert.Null(match.HomeScore);
        Assert.Null(match.AwayScore);
        Assert.Empty(match.Goals);
        Assert.Empty(match.DomainEvents);
    }

    private static void AssertUnfinalized(Match match)
    {
        Assert.Equal(MatchStatus.Scheduled, match.Status);
        Assert.Null(match.HomeScore);
        Assert.Null(match.AwayScore);
        Assert.Empty(match.DomainEvents);
    }
}
