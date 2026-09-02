using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Team : Entity
{
    private readonly List<Player> _players = new();

    public string Name {get; private set;}
    public string ShortName {get; private set;}

    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    private Team()
    {
        Name = string.Empty;
        ShortName = string.Empty;
    }

    private Team(string name, string shortName)
    {
        Name = name;
        ShortName = shortName;
    }

    public static Team Create(string name, string shortName)
    {
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name is required", nameof(name));

        if(string.IsNullOrWhiteSpace(shortName))
            throw new ArgumentException("Team short name is required", nameof(shortName));

        return new Team(
            name.Trim(),
            shortName.Trim().ToUpperInvariant());
    }

    public void Update(string name, string shortName)
    {
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(shortName))
            throw new ArgumentException("Team short name is required.", nameof(shortName));        

        Name = name.Trim();
        ShortName = shortName.Trim().ToUpperInvariant();
    }

    public void AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if(player.TeamId != Id)
            throw new InvalidOperationException("The player does not belomg to this team");

        if(_players.Any(p => p.Id == player.Id))
            return;

        _players.Add(player);
    }
}