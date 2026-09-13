// Responsabilidad del archivo: Cancela de forma lógica e idempotente un partido programado.
// Relación en el sistema: Lee con Dapper, aplica Match.Cancel y persiste el estado Cancelled con EF y Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Matches.Mutations;

public sealed class DeleteMatchCommandHandler(
    IMatchReadRepository reads, IWriteRepository<Match> writes, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteMatchCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        DeleteMatchCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<Guid>.Failure(MatchMutationErrors.NotFound);
        if (snapshot.Status == MatchStatus.Cancelled)
            return Result<Guid>.Success(snapshot.Id);

        var match = UpdateMatchCommandHandler.Restore(snapshot);
        var cancellation = match.Cancel();
        if (cancellation.IsFailure)
            return Result<Guid>.Failure(cancellation.Error!);
        writes.Update(match);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<Guid>.Success(match.Id)
            : Result<Guid>.Failure(commit.Error!);
    }
}
