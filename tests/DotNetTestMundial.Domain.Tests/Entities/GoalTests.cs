using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class GoalTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateGoal()
    {
        var matchId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var goal = Goal.Create(
            matchId,
            playerId,
            45);

        Assert.Equal(matchId, goal.MatchId);
        Assert.Equal(playerId, goal.PlayerId);
        Assert.Equal(45, goal.Minute);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void Create_WithInvalidMinute_ShouldThrow(
        int minute)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Goal.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                minute));
    }
}