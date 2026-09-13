// Responsabilidad del archivo: Verifica las reglas públicas de Team.
// Relación en el sistema: Ejecuta el dominio de forma aislada y previene regresiones antes de persistencia o HTTP.
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Tests.Entities;

public class TeamTests
{
    [Fact]
    public void Create_NormalizesNameAndRaisesCreationEvent()
    {
        var before = DateTime.UtcNow;
        var result = Team.Create("  Tigres  ", " tig ");

        Assert.True(result.IsSuccess);
        var team = result.Value;
        Assert.NotEqual(Guid.Empty, team.Id);
        Assert.Equal("Tigres", team.Name);
        Assert.Equal("TIG", team.ShortName);
        var domainEvent = Assert.IsType<TeamCreatedEvent>(Assert.Single(team.DomainEvents));
        Assert.Equal(team.Id, domainEvent.TeamId);
        Assert.Equal(team.Name, domainEvent.TeamName);
        Assert.Equal(DateTimeKind.Utc, domainEvent.OccurredAt.Kind);
        Assert.InRange(domainEvent.OccurredAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, "TIG", "Team.NameRequired")]
    [InlineData(" ", "TIG", "Team.NameRequired")]
    [InlineData("Tigres", null, "Team.ShortNameRequired")]
    [InlineData("Tigres", " ", "Team.ShortNameRequired")]
    public void Create_InvalidNamesReturnFailure(string? name, string? shortName, string code)
    {
        var result = Team.Create(name, shortName);
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error!.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Update_InvalidShortNamePreservesBothNames()
    {
        var team = Team.Create("Tigres", "TIG").Value;
        team.ClearDomainEvents();

        var result = team.Update("Leones", " ");

        Assert.True(result.IsFailure);
        Assert.Equal("Tigres", team.Name);
        Assert.Equal("TIG", team.ShortName);
        Assert.Empty(team.DomainEvents);
    }

    [Fact]
    public void Update_ValidNamesDoesNotRaiseAnotherCreationEvent()
    {
        var team = Team.Create("Tigres", "TIG").Value;
        team.ClearDomainEvents();
        Assert.True(team.Update(" Leones ", " leo ").IsSuccess);
        Assert.Equal("Leones", team.Name);
        Assert.Equal("LEO", team.ShortName);
        Assert.Empty(team.DomainEvents);
    }

    [Fact]
    public void Restore_RehydratesPersistedIdentityWithoutCreationEvent()
    {
        var id = Guid.NewGuid();

        var team = Team.Restore(id, "Argentina", "ARG");

        Assert.Equal(id, team.Id);
        Assert.Equal("Argentina", team.Name);
        Assert.Equal("ARG", team.ShortName);
        Assert.Empty(team.DomainEvents);
    }

    [Fact]
    public void AddPlayer_RejectsNullAndPlayersFromAnotherTeam()
    {
        var team = Team.Create("Tigres", "TIG").Value;
        var outsider = Player.Create(Guid.NewGuid(), "Jugador", 9).Value;
        Assert.Equal(DomainErrors.PlayerRequired, team.AddPlayer(null).Error);
        Assert.Equal(DomainErrors.PlayerTeamMismatch, team.AddPlayer(outsider).Error);
        Assert.Empty(team.Players);
    }

    [Fact]
    public void AddPlayer_RepeatingAssociationDoesNotDuplicatePlayer()
    {
        var team = Team.Create("Tigres", "TIG").Value;
        var player = Player.Create(team.Id, "Jugador", 9).Value;
        Assert.True(team.AddPlayer(player).IsSuccess);
        Assert.True(team.AddPlayer(player).IsSuccess);
        Assert.Same(player, Assert.Single(team.Players));
    }
}
