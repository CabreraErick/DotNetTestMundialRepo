// Responsabilidad del archivo: Contiene la entrada y salida del caso de uso para crear equipos.
// Relación en el sistema: TeamsController construye el Command y CreateTeamCommandHandler produce el resultado HTTP persistible.
namespace DotNetTestMundial.Application.Teams.CreateTeam;

public sealed record CreateTeamCommand(string? Name, string? ShortName, string? IdempotencyKey);

public sealed record CreateTeamOutcome(Guid TeamId, int StatusCode, string ResponseBody, bool IsReplay);
