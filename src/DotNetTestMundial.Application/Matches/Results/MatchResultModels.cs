// Responsabilidad del archivo: Define contratos de goles, estado completo y registro del marcador.
// Relación en el sistema: Dapper llena las proyecciones, los handlers reconstruyen Domain y API publica sus respuestas.
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Matches.Results;

public sealed record GoalListItem(
    Guid Id,
    Guid MatchId,
    Guid PlayerId,
    string PlayerName,
    Guid TeamId,
    string TeamName,
    int Minute);

public sealed record MatchStateSnapshot(
    MatchListItem Match,
    IReadOnlyList<GoalListItem> Goals);

public sealed record RegisterMatchResultCommand(Guid MatchId, int HomeScore, int AwayScore);

public sealed record MatchResultOutcome(
    Guid MatchId, MatchStatus Status, int HomeScore, int AwayScore);

public static class MatchResultErrors
{
    public static readonly Error PlayerNotFound = new(
        "Goals.PlayerNotFound", "The scorer does not exist.", ErrorType.NotFound);
}
