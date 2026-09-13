// Responsabilidad del archivo: Valida forma y existencia de los dos equipos participantes.
// Relación en el sistema: Los Commands de creación y edición comparten este servicio; las búsquedas se realizan por el puerto Dapper.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Matches.Mutations;

public sealed class MatchTeamValidator(ITeamReadRepository teams, IMatchReadRepository matches)
{
    public async Task<Result> ValidateAsync(
        Guid homeTeamId, Guid awayTeamId, CancellationToken cancellationToken)
    {
        // Reusing Match.Create keeps identifier and distinct-team rules in Domain.
        var shape = Match.Create(homeTeamId, awayTeamId, DateTime.UnixEpoch);
        if (shape.IsFailure)
            return Result.Failure(shape.Error!);
        if (await teams.FindByIdAsync(homeTeamId, cancellationToken) is null)
            return Result.Failure(MatchMutationErrors.HomeTeamNotFound);
        if (await teams.FindByIdAsync(awayTeamId, cancellationToken) is null)
            return Result.Failure(MatchMutationErrors.AwayTeamNotFound);
        return Result.Success();
    }

    public async Task<Result> ValidateScheduleAsync(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt,
        Guid? excludingMatchId = null,
        CancellationToken cancellationToken = default) =>
        await matches.HasTeamScheduleConflictAsync(
            homeTeamId, awayTeamId, scheduledAt, excludingMatchId, cancellationToken)
            ? Result.Failure(MatchMutationErrors.ScheduleConflict)
            : Result.Success();
}
