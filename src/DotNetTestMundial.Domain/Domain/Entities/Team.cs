using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Team : Entity
{
    private readonly List<Player> _players = new();

    public string Name { get; private set; } = string.Empty;
    public string ShortName { get; private set; } = string.Empty;
    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    private Team() { }

    private Team(string name, string shortName)
    {
        Name = name;
        ShortName = shortName;
    }

    public static Result<Team> Create(string? name, string? shortName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Team>.Failure(DomainErrors.TeamNameRequired);
        if (string.IsNullOrWhiteSpace(shortName))
            return Result<Team>.Failure(DomainErrors.TeamShortNameRequired);

        var team = new Team(name.Trim(), shortName.Trim().ToUpperInvariant());
        team.AddDomainEvent(new TeamCreatedEvent(team.Id, team.Name, DateTime.UtcNow));
        return Result<Team>.Success(team);
    }

    public Result Update(string? name, string? shortName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(DomainErrors.TeamNameRequired);
        if (string.IsNullOrWhiteSpace(shortName))
            return Result.Failure(DomainErrors.TeamShortNameRequired);

        Name = name.Trim();
        ShortName = shortName.Trim().ToUpperInvariant();
        return Result.Success();
    }

    public Result AddPlayer(Player? player)
    {
        if (player is null)
            return Result.Failure(DomainErrors.PlayerRequired);
        if (player.TeamId != Id)
            return Result.Failure(DomainErrors.PlayerTeamMismatch);

        if (_players.All(p => p.Id != player.Id))
            _players.Add(player);

        return Result.Success();
    }
}
