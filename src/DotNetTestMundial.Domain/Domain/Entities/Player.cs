using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Player : Entity
{
    private Player()
    {
    }

    private Player(
        Guid id,
        Guid teamId,
        string name,
        int jerseyNumber) : base(id)
    {
        TeamId = teamId;
        Name = name;
        JerseyNumber = jerseyNumber;
        IsActive = true;
    }

    public Guid TeamId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int JerseyNumber { get; private set; }

    public bool IsActive { get; private set; }

    public static Player Create(
        Guid teamId,
        string name,
        int jerseyNumber)
    {
        if (teamId == Guid.Empty)
        {
            throw new ArgumentException(
                "Team id is required.",
                nameof(teamId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Player name cannot be empty.",
                nameof(name));
        }

        if (jerseyNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jerseyNumber),
                "Jersey number must be greater than zero.");
        }

        return new Player(
            Guid.NewGuid(),
            teamId,
            name.Trim(),
            jerseyNumber);
    }

    public void Update(
        string name,
        int jerseyNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Player name cannot be empty.",
                nameof(name));
        }

        if (jerseyNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jerseyNumber),
                "Jersey number must be greater than zero.");
        }

        Name = name.Trim();
        JerseyNumber = jerseyNumber;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}