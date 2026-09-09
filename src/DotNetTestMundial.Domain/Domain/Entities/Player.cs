// Responsabilidad del archivo: Modela un jugador registrado en un equipo y su estado activo.
// Relación en el sistema: Team y Goal validan su pertenencia; PlayerConfiguration define su persistencia.
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

    /// <summary>
    /// Reconstructs a persisted player for a Command without treating the read as a new registration.
    /// Application obtains the snapshot with Dapper and EF Core later persists only the requested change.
    /// </summary>
    public static Player Restore(
        Guid id, Guid teamId, string name, int jerseyNumber, bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A persisted player must have an identifier.", nameof(id));
        if (teamId == Guid.Empty)
            throw new ArgumentException("A persisted player must belong to a team.", nameof(teamId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (jerseyNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(jerseyNumber));

        return new Player(teamId, name, jerseyNumber) { Id = id, IsActive = isActive };
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
