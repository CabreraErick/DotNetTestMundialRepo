using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class PlayerTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateActivePlayer()
    {
        var teamId = Guid.NewGuid();

        var player = Player.Create(
            teamId,
            "Juan Perez",
            10);

        Assert.Equal(teamId, player.TeamId);
        Assert.Equal("Juan Perez", player.Name);
        Assert.Equal(10, player.JerseyNumber);
        Assert.True(player.IsActive);
    }

    [Fact]
    public void Create_WithInvalidJerseyNumber_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Player.Create(
                Guid.NewGuid(),
                "Juan Perez",
                0));
    }

    [Fact]
    public void Deactivate_ShouldSetPlayerAsInactive()
    {
        var player = Player.Create(
            Guid.NewGuid(),
            "Juan Perez",
            10);

        player.Deactivate();

        Assert.False(player.IsActive);
    }
}