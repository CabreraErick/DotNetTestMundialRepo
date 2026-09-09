// Responsabilidad del archivo: Declara la solicitud idempotente para registrar un gol y su respuesta HTTP persistida.
// Relación en el sistema: MatchesController crea el Command y CreateGoalCommandHandler coordina Domain, EF y Unit of Work.
namespace DotNetTestMundial.Application.Matches.Results;

public sealed record CreateGoalCommand(
    Guid MatchId, Guid PlayerId, int Minute, string? IdempotencyKey);

public sealed record CreateGoalOutcome(
    Guid GoalId, int StatusCode, string ResponseBody, bool IsReplay);
