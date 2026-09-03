using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Team : Entity
{
    private Team()
    {
    }

    private Team(
        Guid id,
        string name,
        string shortName) : base(id)
    {
        Name = name;
        ShortName = shortName;
    }

    public string Name { get; private set; } = string.Empty;

    public string ShortName { get; private set; } = string.Empty;

    public static Team Create(
        string name,
        string shortName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Team name cannot be empty.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(shortName))
        {
            throw new ArgumentException(
                "Team short name cannot be empty.",
                nameof(shortName));
        }

        var team = new Team(
            Guid.NewGuid(),
            name.Trim(),
            shortName.Trim().ToUpperInvariant());

        team.RaiseDomainEvent(
            new TeamCreatedEvent(
                team.Id,
                team.Name,
                DateTime.UtcNow));

        return team;
    }

    public void Update(
        string name,
        string shortName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Team name cannot be empty.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(shortName))
        {
            throw new ArgumentException(
                "Team short name cannot be empty.",
                nameof(shortName));
        }

        Name = name.Trim();
        ShortName = shortName.Trim().ToUpperInvariant();
    }
}