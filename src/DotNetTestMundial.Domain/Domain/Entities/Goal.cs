using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Goal : Entity
{
    public Guid MatchId { get; private set; }

    public Guid PlayerId { get; private set; }

    public int Minute { get; private set; }

    private Goal()
    {
    }

    private Goal(Guid matchId, Guid playerId, int minute)
    {
        MatchId = matchId;
        PlayerId = playerId;
        Minute = minute;
    }

    public static Goal Create(Guid matchId, Guid playerId, int minute)
    {
        if (matchId == Guid.Empty)
            throw new ArgumentException(
                "Match is required.",
                nameof(matchId));

        if (playerId == Guid.Empty)
            throw new ArgumentException(
                "Player is required.",
                nameof(playerId));

        if (minute < 1 || minute > 120)
            throw new ArgumentOutOfRangeException(
                nameof(minute),
                "Goal minute must be between 1 and 120.");

        return new Goal(matchId,playerId,minute);
    }
}

/*
    Se considera un maximo de 120 minutos por tiempo reglamentario mas tiempo extra
*/