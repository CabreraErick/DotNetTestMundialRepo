// Responsabilidad del archivo: Comparte Commands, respuestas y errores para reprogramar o cancelar partidos.
// Relación en el sistema: MatchesController construye estos contratos y los handlers aplican reglas de Match antes del commit.
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Matches.Mutations;

public sealed record UpdateMatchCommand(
    Guid Id, Guid HomeTeamId, Guid AwayTeamId, DateTime ScheduledAt);
public sealed record PatchMatchCommand(
    Guid Id, Guid? HomeTeamId, Guid? AwayTeamId, DateTime? ScheduledAt);
public sealed record DeleteMatchCommand(Guid Id);
public sealed record MatchMutationResult(
    Guid Id,
    Guid HomeTeamId,
    Guid AwayTeamId,
    DateTime ScheduledAt,
    MatchStatus Status,
    int? HomeScore,
    int? AwayScore);

public static class MatchMutationErrors
{
    public static readonly Error NotFound = new(
        "Matches.NotFound", "The requested match does not exist.", ErrorType.NotFound);
    public static readonly Error HomeTeamNotFound = new(
        "Matches.HomeTeamNotFound", "The home team does not exist.", ErrorType.NotFound);
    public static readonly Error AwayTeamNotFound = new(
        "Matches.AwayTeamNotFound", "The away team does not exist.", ErrorType.NotFound);
    public static readonly Error PatchEmpty = new(
        "Matches.PatchEmpty", "Patch must provide homeTeamId, awayTeamId or scheduledAt.", ErrorType.Validation);
    public static readonly Error TeamsLockedByGoals = new(
        "Matches.TeamsLockedByGoals", "Teams cannot change after goals have been registered.", ErrorType.Conflict);
}
