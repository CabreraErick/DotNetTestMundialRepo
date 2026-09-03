using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Entities;

public sealed class Goal : Entity
{
    private Goal()
    {
    }

    private Goal(
        Guid id,
        Guid matchId,
        Guid playerId,
        int minute) : base(id)
    {
        MatchId = matchId;
        PlayerId = playerId;
        Minute = minute;
    }

    public Guid MatchId { get; private set; }

    public Guid PlayerId { get; private set; }

    public int Minute { get; private set; }

    public static Goal Create(
        Guid matchId,
        Guid playerId,
        int minute)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException(
                "Match id is required.",
                nameof(matchId));
        }

        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player id is required.",
                nameof(playerId));
        }

        if (minute is < 1 or > 120)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minute),
                "Goal minute must be between 1 and 120.");
        }

        return new Goal(
            Guid.NewGuid(),
            matchId,
            playerId,
            minute);
    }
}

// Se considera un rango de 120 minutos maximo con tiempo extra incluido