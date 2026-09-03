using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Enums;
using DotNetTestMundial.Domain.Events;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Match : Entity
{
    private Match()
    {
    }

    private Match(
        Guid id,
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt) : base(id)
    {
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        ScheduledAt = scheduledAt;
        Status = MatchStatus.Scheduled;
    }

    public Guid HomeTeamId { get; private set; }

    public Guid AwayTeamId { get; private set; }

    public DateTime ScheduledAt { get; private set; }

    public MatchStatus Status { get; private set; }

    public int? HomeScore { get; private set; }

    public int? AwayScore { get; private set; }

    public static Match Create(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt)
    {
        if (homeTeamId == Guid.Empty)
        {
            throw new ArgumentException(
                "Home team id is required.",
                nameof(homeTeamId));
        }

        if (awayTeamId == Guid.Empty)
        {
            throw new ArgumentException(
                "Away team id is required.",
                nameof(awayTeamId));
        }

        if (homeTeamId == awayTeamId)
        {
            throw new ArgumentException(
                "A team cannot play against itself.");
        }

        return new Match(
            Guid.NewGuid(),
            homeTeamId,
            awayTeamId,
            scheduledAt);
    }

    public void RegisterResult(
        int homeScore,
        int awayScore)
    {
        if (Status == MatchStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cannot register a result for a cancelled match.");
        }

        if (homeScore < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(homeScore));
        }

        if (awayScore < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(awayScore));
        }

        HomeScore = homeScore;
        AwayScore = awayScore;
        Status = MatchStatus.Played;

        RaiseDomainEvent(
            new MatchResultRegisteredEvent(
                Id,
                homeScore,
                awayScore,
                DateTime.UtcNow));
    }

    public void Reschedule(DateTime scheduledAt)
    {
        if (Status != MatchStatus.Scheduled)
        {
            throw new InvalidOperationException(
                "Only scheduled matches can be rescheduled.");
        }

        ScheduledAt = scheduledAt;
    }

    public void Cancel()
    {
        if (Status == MatchStatus.Played)
        {
            throw new InvalidOperationException(
                "A played match cannot be cancelled.");
        }

        Status = MatchStatus.Cancelled;
    }
}