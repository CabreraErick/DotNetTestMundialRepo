// Responsabilidad del archivo: Modifica parcialmente equipos o fecha de un partido programado.
// Relación en el sistema: Combina campos omitidos con el snapshot Dapper y reutiliza validación y persistencia del PUT.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Matches.Mutations;

public sealed class PatchMatchCommandHandler(
    IMatchReadRepository reads,
    MatchTeamValidator teamValidator,
    IWriteRepository<Match> writes,
    IUnitOfWork unitOfWork) : ICommandHandler<PatchMatchCommand, MatchMutationResult>
{
    public async Task<Result<MatchMutationResult>> HandleAsync(
        PatchMatchCommand command, CancellationToken cancellationToken = default)
    {
        if (command.HomeTeamId is null && command.AwayTeamId is null && command.ScheduledAt is null)
            return Result<MatchMutationResult>.Failure(MatchMutationErrors.PatchEmpty);
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<MatchMutationResult>.Failure(MatchMutationErrors.NotFound);

        var homeTeamId = command.HomeTeamId ?? snapshot.HomeTeamId;
        var awayTeamId = command.AwayTeamId ?? snapshot.AwayTeamId;
        var match = UpdateMatchCommandHandler.Restore(snapshot);
        var update = match.Reschedule(
            homeTeamId,
            awayTeamId,
            command.ScheduledAt ?? snapshot.ScheduledAt);
        if (update.IsFailure)
            return Result<MatchMutationResult>.Failure(update.Error!);
        var preliminary = UpdateMatchCommandHandler.ValidateTeamChange(snapshot, homeTeamId, awayTeamId);
        if (preliminary.IsFailure)
            return Result<MatchMutationResult>.Failure(preliminary.Error!);
        var existence = await teamValidator.ValidateAsync(
            homeTeamId, awayTeamId, cancellationToken);
        if (existence.IsFailure)
            return Result<MatchMutationResult>.Failure(existence.Error!);

        writes.Update(match);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<MatchMutationResult>.Success(UpdateMatchCommandHandler.ToResult(match))
            : Result<MatchMutationResult>.Failure(commit.Error!);
    }
}
