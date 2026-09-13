// Responsabilidad del archivo: Define la entrada y respuesta persistible para registrar un jugador.
// Relación en el sistema: PlayersController aporta el Idempotency-Key y el handler devuelve el cuerpo HTTP original o repetido.
namespace DotNetTestMundial.Application.Players.CreatePlayer;

public sealed record CreatePlayerCommand(
    Guid TeamId, string? Name, int JerseyNumber, string? IdempotencyKey);

public sealed record CreatePlayerOutcome(
    Guid PlayerId, int StatusCode, string ResponseBody, bool IsReplay);
