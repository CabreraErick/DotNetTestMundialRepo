// Responsabilidad del archivo: Modela calendario, estado, goles y resultado coherente de un partido.
// Relación en el sistema: Application lo utilizará en Commands y MatchConfiguration conserva sus relaciones.
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Enums;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Match : Entity
{
    private readonly List<Goal> _goals = new();

    public Guid HomeTeamId { get; private set; }
    public Guid AwayTeamId { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public MatchStatus Status { get; private set; }
    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }
    public IReadOnlyCollection<Goal> Goals => _goals.AsReadOnly();

    private Match() { }

    private Match(Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt)
    {
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        ScheduledAt = scheduledAt;
        Status = MatchStatus.Scheduled;
    }

    /// <summary>
    /// Reconstructs the persisted scalar state used by scheduling Commands. Goals are read
    /// separately by Dapper and are not needed to cancel or reprogram an eligible match.
    /// </summary>
    public static Match Restore(
        Guid id,
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt,
        MatchStatus status,
        int? homeScore,
        int? awayScore)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A persisted match must have an identifier.", nameof(id));
        if (homeTeamId == Guid.Empty || awayTeamId == Guid.Empty || homeTeamId == awayTeamId)
            throw new ArgumentException("Persisted match teams are invalid.", nameof(homeTeamId));
        if (scheduledAt == default)
            throw new ArgumentException("A persisted match must have a scheduled date.", nameof(scheduledAt));
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));
        var played = status == MatchStatus.Played;
        var hasBothScores = homeScore.HasValue && awayScore.HasValue;
        var hasAnyScore = homeScore.HasValue || awayScore.HasValue;
        if ((played && (!hasBothScores || homeScore < 0 || awayScore < 0)) ||
            (!played && hasAnyScore))
            throw new ArgumentException("Persisted score and status are inconsistent.", nameof(status));

        return new Match(homeTeamId, awayTeamId, scheduledAt)
        {
            Id = id,
            Status = status,
            HomeScore = homeScore,
            AwayScore = awayScore
        };
    }

    public static Result<Match> Create(Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt)
    {
        if (homeTeamId == Guid.Empty)
            return Result<Match>.Failure(DomainErrors.HomeTeamRequired);
        if (awayTeamId == Guid.Empty)
            return Result<Match>.Failure(DomainErrors.AwayTeamRequired);
        if (homeTeamId == awayTeamId)
            return Result<Match>.Failure(DomainErrors.SameTeams);
        if (scheduledAt == default)
            return Result<Match>.Failure(DomainErrors.ScheduledAtRequired);

        return Result<Match>.Success(new Match(homeTeamId, awayTeamId, scheduledAt));
    }

    /// <summary>Changes participants and date only while the match is still scheduled.</summary>
    public Result Reschedule(Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt)
    {
        if (Status != MatchStatus.Scheduled)
            return Result.Failure(DomainErrors.MatchNotScheduled);
        if (homeTeamId == Guid.Empty)
            return Result.Failure(DomainErrors.HomeTeamRequired);
        if (awayTeamId == Guid.Empty)
            return Result.Failure(DomainErrors.AwayTeamRequired);
        if (homeTeamId == awayTeamId)
            return Result.Failure(DomainErrors.SameTeams);
        if (scheduledAt == default)
            return Result.Failure(DomainErrors.ScheduledAtRequired);

        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        ScheduledAt = scheduledAt;
        return Result.Success();
    }

    public Result RegisterResult(int homeScore, int awayScore)
    {
        if (Status != MatchStatus.Scheduled)
            return Result.Failure(DomainErrors.MatchNotScheduled);
        if (homeScore < 0 || awayScore < 0)
            return Result.Failure(DomainErrors.NegativeScore);
        if (_goals.Count(g => g.TeamId == HomeTeamId) != homeScore ||
            _goals.Count(g => g.TeamId == AwayTeamId) != awayScore)
            return Result.Failure(DomainErrors.ScoreMismatch);

        HomeScore = homeScore;
        AwayScore = awayScore;
        Status = MatchStatus.Played;
        AddDomainEvent(new MatchResultRegisteredEvent(Id, homeScore, awayScore, DateTime.UtcNow));
        return Result.Success();
    }

    public Result AddGoal(Goal? goal)
    {
        if (goal is null)
            return Result.Failure(DomainErrors.GoalRequired);
        if (Status != MatchStatus.Scheduled)
            return Result.Failure(DomainErrors.MatchNotScheduled);
        if (goal.MatchId != Id)
            return Result.Failure(DomainErrors.GoalMatchMismatch);
        if (goal.TeamId != HomeTeamId && goal.TeamId != AwayTeamId)
            return Result.Failure(DomainErrors.ScorerTeamMismatch);
        if (_goals.Any(g => g.Id == goal.Id))
            return Result.Failure(DomainErrors.DuplicateGoal);

        _goals.Add(goal);
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != MatchStatus.Scheduled)
            return Result.Failure(DomainErrors.MatchNotScheduled);

        Status = MatchStatus.Cancelled;
        return Result.Success();
    }
}
