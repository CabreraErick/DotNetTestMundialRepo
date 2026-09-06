using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Player : Entity
{
    public Guid TeamId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int JerseyNumber { get; private set; }
    public bool IsActive { get; private set; }

    private Player() { }

    private Player(Guid teamId, string name, int jerseyNumber)
    {
        TeamId = teamId;
        Name = name;
        JerseyNumber = jerseyNumber;
        IsActive = true;
    }

    public static Result<Player> Create(Guid teamId, string? name, int jerseyNumber)
    {
        if (teamId == Guid.Empty)
            return Result<Player>.Failure(DomainErrors.TeamRequired);
        if (string.IsNullOrWhiteSpace(name))
            return Result<Player>.Failure(DomainErrors.PlayerNameRequired);
        if (jerseyNumber <= 0)
            return Result<Player>.Failure(DomainErrors.InvalidJerseyNumber);

        return Result<Player>.Success(new Player(teamId, name.Trim(), jerseyNumber));
    }

    public Result Update(string? name, int jerseyNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(DomainErrors.PlayerNameRequired);
        if (jerseyNumber <= 0)
            return Result.Failure(DomainErrors.InvalidJerseyNumber);

        Name = name.Trim();
        JerseyNumber = jerseyNumber;
        return Result.Success();
    }

    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }
}
