using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Player : Entity
{
    public Guid TeamId {get; private set;}
    public string Name { get; private set; }
    public int JerseyNumber { get; private set; }
    public bool IsActive { get; private set; }    

    private Player()
    {
        Name = string.Empty;
    }

    private Player(Guid teamId, string name, int jerseyNumber)
    {
        TeamId = teamId;
        Name = name;
        JerseyNumber = jerseyNumber;
        IsActive = true;
    }

    public static Player Create(Guid teamId, string name, int jerseyNumber)
    {
        if(teamId == Guid.Empty)
        {
            throw new ArgumentException("Team is required", nameof(teamId));
        }
        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Player name is required", nameof(name));
        }   
        if(jerseyNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jerseyNumber), "Jersey number must be more than zero");
        }     

        return new Player(teamId,name.Trim(),jerseyNumber);
    }

    public void Update(string name, int jerseyNumber)
    {
        if(string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Player name is required", nameof(name));
        }

        if(jerseyNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jerseyNumber));
        }

        Name = name.Trim();
        JerseyNumber = jerseyNumber;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}