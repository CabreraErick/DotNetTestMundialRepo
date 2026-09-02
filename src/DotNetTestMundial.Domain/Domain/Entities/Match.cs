using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Enums;

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

    private Match()
    {
    }

    private Match(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt)
    {
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        ScheduledAt = scheduledAt;
        Status = MatchStatus.Scheduled;
    }

    public static Match Create(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt)
    {
        if (homeTeamId == Guid.Empty)
            throw new ArgumentException(
                "Home team is required.",
                nameof(homeTeamId));

        if (awayTeamId == Guid.Empty)
            throw new ArgumentException(
                "Away team is required.",
                nameof(awayTeamId));

        if (homeTeamId == awayTeamId)
            throw new ArgumentException(
                "A team cannot play against itself.");

        return new Match(
            homeTeamId,
            awayTeamId,
            scheduledAt);
    }

    public void RegisterResult(
        int homeScore,
        int awayScore)
    {
        if (Status != MatchStatus.Scheduled)
            throw new InvalidOperationException(
                "Only scheduled matches can receive a result.");

        if (homeScore < 0)
            throw new ArgumentOutOfRangeException(
                nameof(homeScore));

        if (awayScore < 0)
            throw new ArgumentOutOfRangeException(
                nameof(awayScore));

        HomeScore = homeScore;
        AwayScore = awayScore;
        Status = MatchStatus.Played;
    }

    public void AddGoal(Goal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);

        if (Status != MatchStatus.Scheduled)
            throw new InvalidOperationException(
                "Goals can only be registered before the match result is finalized.");

        if (goal.MatchId != Id)
            throw new InvalidOperationException(
                "The goal does not belong to this match.");

        _goals.Add(goal);
    }

    public void Cancel()
    {
        if (Status != MatchStatus.Scheduled)
            throw new InvalidOperationException(
                "Only scheduled matches can be cancelled.");

        Status = MatchStatus.Cancelled;
    }
}