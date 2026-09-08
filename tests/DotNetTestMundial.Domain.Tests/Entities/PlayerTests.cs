// Responsabilidad del archivo: Verifica las reglas públicas de Player.
// Relación en el sistema: Ejecuta el dominio de forma aislada y previene regresiones antes de persistencia o HTTP.
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class PlayerTests
{
    [Fact]
    public void Create_PreservesTeamAndActivatesPlayer()
    {
        var teamId = Guid.NewGuid();
        var player = Player.Create(teamId, " Ana ", 10).Value;
        Assert.Equal(teamId, player.TeamId);
        Assert.Equal("Ana", player.Name);
        Assert.Equal(10, player.JerseyNumber);
        Assert.True(player.IsActive);
    }

    [Fact]
    public void Create_RejectsMissingTeam()
    {
        Assert.Equal(DomainErrors.TeamRequired, Player.Create(Guid.Empty, "Ana", 10).Error);
    }

    [Theory]
    [InlineData(null, 10, "Player.NameRequired")]
    [InlineData(" ", 10, "Player.NameRequired")]
    [InlineData("Ana", 0, "Player.InvalidJerseyNumber")]
    [InlineData("Ana", -1, "Player.InvalidJerseyNumber")]
    public void Create_RejectsInvalidInput(string? name, int jerseyNumber, string code)
    {
        var result = Player.Create(Guid.NewGuid(), name, jerseyNumber);
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error!.Code);
    }

    [Theory]
    [InlineData(null, 20)]
    [InlineData("Otro", 0)]
    public void Update_InvalidInputPreservesPlayer(string? name, int jerseyNumber)
    {
        var player = Player.Create(Guid.NewGuid(), "Ana", 10).Value;
        Assert.True(player.Update(name, jerseyNumber).IsFailure);
        Assert.Equal("Ana", player.Name);
        Assert.Equal(10, player.JerseyNumber);
    }

    [Fact]
    public void Update_ChangesNameAndNumberWithoutChangingTeam()
    {
        var teamId = Guid.NewGuid();
        var player = Player.Create(teamId, "Ana", 10).Value;
        Assert.True(player.Update(" Ana Maria ", 20).IsSuccess);
        Assert.Equal("Ana Maria", player.Name);
        Assert.Equal(20, player.JerseyNumber);
        Assert.Equal(teamId, player.TeamId);
    }

    [Fact]
    public void Deactivate_IsRepeatable()
    {
        var player = Player.Create(Guid.NewGuid(), "Ana", 10).Value;
        Assert.True(player.Deactivate().IsSuccess);
        Assert.True(player.Deactivate().IsSuccess);
        Assert.False(player.IsActive);
    }
}
