// Responsabilidad del archivo: Registra el marcador final únicamente cuando coincide con los goles persistidos.
// Relación en el sistema: Dapper reconstruye el agregado Match y EF Core actualiza sus escalares mediante un solo Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Matches.Results;

public sealed class RegisterMatchResultCommandHandler(
    IMatchReadRepository reads,
    IWriteRepository<Match> writes,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterMatchResultCommand, MatchResultOutcome>
{
    public async Task<Result<MatchResultOutcome>> HandleAsync(
        RegisterMatchResultCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await reads.FindStateByIdAsync(command.MatchId, cancellationToken);
        if (snapshot is null)
            return Result<MatchResultOutcome>.Failure(MatchMutationErrors.NotFound);

        var match = UpdateMatchCommandHandler.Restore(snapshot.Match);
        foreach (var item in snapshot.Goals)
        {
            var goal = Goal.Restore(
                item.Id, item.MatchId, item.PlayerId, item.TeamId, item.Minute);
            var addition = match.AddGoal(goal);
            if (addition.IsFailure)
                return Result<MatchResultOutcome>.Failure(addition.Error!);
        }

        var registration = match.RegisterResult(command.HomeScore, command.AwayScore);
        if (registration.IsFailure)
            return Result<MatchResultOutcome>.Failure(registration.Error!);

        writes.Update(match);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<MatchResultOutcome>.Success(new(
                match.Id, match.Status, match.HomeScore!.Value, match.AwayScore!.Value))
            : Result<MatchResultOutcome>.Failure(commit.Error!);
    }
}
