using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class GoalTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public void Create_ValidGoalCapturesScorerAndTeam(int minute)
    {
        var player = Player.Create(Guid.NewGuid(), "Ana", 10).Value;
        var matchId = Guid.NewGuid();
        var goal = Goal.Create(matchId, player, minute).Value;
        Assert.Equal(matchId, goal.MatchId);
        Assert.Equal(player.Id, goal.PlayerId);
        Assert.Equal(player.TeamId, goal.TeamId);
        Assert.Equal(minute, goal.Minute);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(121)]
    public void Create_RejectsInvalidMinute(int minute)
    {
        var player = Player.Create(Guid.NewGuid(), "Ana", 10).Value;
        Assert.Equal(DomainErrors.InvalidGoalMinute, Goal.Create(Guid.NewGuid(), player, minute).Error);
    }

    [Fact]
    public void Create_RejectsMissingMatchAndPlayer()
    {
        var player = Player.Create(Guid.NewGuid(), "Ana", 10).Value;
        Assert.Equal(DomainErrors.MatchRequired, Goal.Create(Guid.Empty, player, 10).Error);
        Assert.Equal(DomainErrors.PlayerRequired, Goal.Create(Guid.NewGuid(), null, 10).Error);
    }

    [Fact]
    public void Create_RejectsInactiveScorer()
    {
        var player = Player.Create(Guid.NewGuid(), "Ana", 10).Value;
        player.Deactivate();
        var result = Goal.Create(Guid.NewGuid(), player, 10);
        Assert.Equal(DomainErrors.InactivePlayer, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }
}
