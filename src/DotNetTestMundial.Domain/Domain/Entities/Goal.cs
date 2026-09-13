// Responsabilidad del archivo: Modela un gol con autor, equipo, partido y minuto.
// Relación en el sistema: Match valida su incorporación y GoalConfiguration garantiza sus referencias en SQL Server.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Goal : Entity
{
    public Guid MatchId { get; private set; }
    public Guid PlayerId { get; private set; }
    // Keep the scoring team at registration time for per-team score validation.
    public Guid TeamId { get; private set; }
    public int Minute { get; private set; }

    private Goal() { }

    private Goal(Guid matchId, Player scorer, int minute)
    {
        MatchId = matchId;
        PlayerId = scorer.Id;
        TeamId = scorer.TeamId;
        Minute = minute;
    }

    /// <summary>
    /// Reconstructs a persisted goal so a result Command can verify the score through
    /// Match without asking EF Core to perform a read.
    /// </summary>
    public static Goal Restore(Guid id, Guid matchId, Guid playerId, Guid teamId, int minute)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A persisted goal must have an identifier.", nameof(id));
        if (matchId == Guid.Empty)
            throw new ArgumentException("A persisted goal must belong to a match.", nameof(matchId));
        if (playerId == Guid.Empty)
            throw new ArgumentException("A persisted goal must have a scorer.", nameof(playerId));
        if (teamId == Guid.Empty)
            throw new ArgumentException("A persisted goal must belong to a team.", nameof(teamId));
        if (minute < 1 || minute > 120)
            throw new ArgumentOutOfRangeException(nameof(minute));

        var scorer = Player.Restore(playerId, teamId, "Persisted scorer", 1, true);
        return new Goal(matchId, scorer, minute) { Id = id };
    }

    public static Result<Goal> Create(Guid matchId, Player? scorer, int minute)
    {
        if (matchId == Guid.Empty)
            return Result<Goal>.Failure(DomainErrors.MatchRequired);
        if (scorer is null)
            return Result<Goal>.Failure(DomainErrors.PlayerRequired);
        if (!scorer.IsActive)
            return Result<Goal>.Failure(DomainErrors.InactivePlayer);
        if (minute < 1 || minute > 120)
            return Result<Goal>.Failure(DomainErrors.InvalidGoalMinute);

        return Result<Goal>.Success(new Goal(matchId, scorer, minute));
    }
}
