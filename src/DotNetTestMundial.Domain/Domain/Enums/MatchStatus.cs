namespace DotNetTestMundial.Domain.Enums;

public enum MatchStatus
{
    Scheduled = 1,
    Played = 2,
    Cancelled = 3
}

/*
    Permite representar el estado del partido
    Proceso:
    Scheduled -> RegisterResult
        -> Played

    Scheduled -> Cancel
        -> CAncelled
*/