// Responsabilidad del archivo: Ejecuta la baja lógica e idempotente de un jugador.
// Relación en el sistema: Reconstruye el snapshot Dapper, usa Player.Deactivate y persiste IsActive=false con EF y Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Players.Mutations;

public sealed class DeletePlayerCommandHandler(
    IPlayerReadRepository reads, IWriteRepository<Player> writes, IUnitOfWork unitOfWork)
    : ICommandHandler<DeletePlayerCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        DeletePlayerCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<Guid>.Failure(PlayerMutationErrors.NotFound);
        if (!snapshot.IsActive)
            return Result<Guid>.Success(snapshot.Id);

        var player = Player.Restore(snapshot.Id, snapshot.TeamId, snapshot.Name,
            snapshot.JerseyNumber, snapshot.IsActive);
        player.Deactivate();
        writes.Update(player);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<Guid>.Success(player.Id)
            : Result<Guid>.Failure(commit.Error!);
    }
}
