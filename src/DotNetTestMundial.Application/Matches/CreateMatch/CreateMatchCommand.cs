// Responsabilidad del archivo: Define la entrada y respuesta persistible para programar un partido.
// Relación en el sistema: MatchesController aporta Idempotency-Key y el handler conserva la respuesta HTTP original.
namespace DotNetTestMundial.Application.Matches.CreateMatch;

public sealed record CreateMatchCommand(
    Guid HomeTeamId,
    Guid AwayTeamId,
    DateTime ScheduledAt,
    string? IdempotencyKey);

public sealed record CreateMatchOutcome(
    Guid MatchId, int StatusCode, string ResponseBody, bool IsReplay);
