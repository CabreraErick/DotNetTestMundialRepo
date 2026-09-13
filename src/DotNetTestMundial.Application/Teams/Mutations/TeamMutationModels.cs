// Responsabilidad del archivo: Comparte contratos de entrada, salida y errores de las escrituras REST de equipos.
// Relación en el sistema: TeamsController crea Commands; sus handlers usan Dapper, Domain y Unit of Work.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Teams.Mutations;

public sealed record UpdateTeamCommand(Guid Id, string? Name, string? ShortName);
public sealed record PatchTeamCommand(Guid Id, string? Name, string? ShortName);
public sealed record DeleteTeamCommand(Guid Id);
public sealed record TeamMutationResult(Guid Id, string Name, string ShortName);

public static class TeamMutationErrors
{
    public static readonly Error NotFound = new(
        "Teams.NotFound", "The requested team does not exist.", ErrorType.NotFound);
    public static readonly Error PatchEmpty = new(
        "Teams.PatchEmpty", "Patch must provide name or shortName.", ErrorType.Validation);
    public static readonly Error NameAlreadyExists = new(
        "Teams.NameAlreadyExists", "A team with this name already exists.", ErrorType.Conflict);
    public static readonly Error ShortNameAlreadyExists = new(
        "Teams.ShortNameAlreadyExists", "A team with this short name already exists.", ErrorType.Conflict);
}
