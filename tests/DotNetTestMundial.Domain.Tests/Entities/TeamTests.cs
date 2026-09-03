using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class TeamTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateTeam()
    {
        var team = Team.Create(
            "Aguilas FC",
            "agf");

        Assert.NotEqual(Guid.Empty, team.Id);
        Assert.Equal("Aguilas FC", team.Name);
        Assert.Equal("AGF", team.ShortName);
    }

    [Fact]
    public void Create_ShouldRaiseTeamCreatedEvent()
    {
        var team = Team.Create(
            "Aguilas FC",
            "AGF");

        Assert.Single(team.DomainEvents);

        Assert.IsType<TeamCreatedEvent>(
            team.DomainEvents.First());
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Team.Create("", "AGF"));
    }
}