// Responsabilidad del archivo: Reemplaza equipos y fecha de un partido todavía programado.
// Relación en el sistema: Lee el snapshot con Dapper, valida equipos, aplica Match.Reschedule y confirma con Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Matches.Mutations;

public sealed class UpdateMatchCommandHandler(
    IMatchReadRepository reads,
    MatchTeamValidator teamValidator,
    IWriteRepository<Match> writes,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateMatchCommand, MatchMutationResult>
{
    public async Task<Result<MatchMutationResult>> HandleAsync(
        UpdateMatchCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<MatchMutationResult>.Failure(MatchMutationErrors.NotFound);
        var match = Restore(snapshot);
        var update = match.Reschedule(command.HomeTeamId, command.AwayTeamId, command.ScheduledAt);
        if (update.IsFailure)
            return Result<MatchMutationResult>.Failure(update.Error!);
        var preliminary = ValidateTeamChange(snapshot, command.HomeTeamId, command.AwayTeamId);
        if (preliminary.IsFailure)
            return Result<MatchMutationResult>.Failure(preliminary.Error!);

        var existence = await teamValidator.ValidateAsync(
            command.HomeTeamId, command.AwayTeamId, cancellationToken);
        if (existence.IsFailure)
            return Result<MatchMutationResult>.Failure(existence.Error!);

        writes.Update(match);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<MatchMutationResult>.Success(ToResult(match))
            : Result<MatchMutationResult>.Failure(commit.Error!);
    }

    internal static Match Restore(MatchListItem snapshot) => Match.Restore(
        snapshot.Id,
        snapshot.HomeTeamId,
        snapshot.AwayTeamId,
        snapshot.ScheduledAt,
        snapshot.Status,
        snapshot.HomeScore,
        snapshot.AwayScore);

    internal static MatchMutationResult ToResult(Match match) => new(
        match.Id,
        match.HomeTeamId,
        match.AwayTeamId,
        match.ScheduledAt,
        match.Status,
        match.HomeScore,
        match.AwayScore);

    internal static Result ValidateTeamChange(
        MatchListItem snapshot, Guid homeTeamId, Guid awayTeamId) =>
        snapshot.GoalCount > 0 &&
        (homeTeamId != snapshot.HomeTeamId || awayTeamId != snapshot.AwayTeamId)
            ? Result.Failure(MatchMutationErrors.TeamsLockedByGoals)
            : Result.Success();

}
