// Responsabilidad del archivo: Comparte contratos y errores de actualización y baja lógica de jugadores.
// Relación en el sistema: API crea Commands y los handlers devuelven estas proyecciones mediante Result.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Players.Mutations;

public sealed record UpdatePlayerCommand(Guid Id, string? Name, int JerseyNumber);
public sealed record PatchPlayerCommand(Guid Id, string? Name, int? JerseyNumber, bool? IsActive);
public sealed record DeletePlayerCommand(Guid Id);
public sealed record PlayerMutationResult(
    Guid Id, Guid TeamId, string Name, int JerseyNumber, bool IsActive);

public static class PlayerMutationErrors
{
    public static readonly Error NotFound = new(
        "Players.NotFound", "The requested player does not exist.", ErrorType.NotFound);
    public static readonly Error TeamNotFound = new(
        "Players.TeamNotFound", "The requested team does not exist.", ErrorType.NotFound);
    public static readonly Error PatchEmpty = new(
        "Players.PatchEmpty", "Patch must provide name, jerseyNumber or isActive.", ErrorType.Validation);
    public static readonly Error JerseyNumberAlreadyAssigned = new(
        "Players.JerseyNumberAlreadyAssigned",
        "This jersey number is already assigned to another player in the team.",
        ErrorType.Conflict);
}
