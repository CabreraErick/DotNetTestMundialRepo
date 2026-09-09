// Responsabilidad del archivo: Ejecuta cambios parciales de nombre o dorsal conservando los campos omitidos.
// Relación en el sistema: Combina la proyección Dapper con el PATCH y reutiliza las invariantes de Player.Update.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Players.Mutations;

public sealed class PatchPlayerCommandHandler(
    IPlayerReadRepository reads, IWriteRepository<Player> writes, IUnitOfWork unitOfWork)
    : ICommandHandler<PatchPlayerCommand, PlayerMutationResult>
{
    public async Task<Result<PlayerMutationResult>> HandleAsync(
        PatchPlayerCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Name is null && command.JerseyNumber is null)
            return Result<PlayerMutationResult>.Failure(PlayerMutationErrors.PatchEmpty);
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<PlayerMutationResult>.Failure(PlayerMutationErrors.NotFound);
        var player = Player.Restore(snapshot.Id, snapshot.TeamId, snapshot.Name,
            snapshot.JerseyNumber, snapshot.IsActive);
        var update = player.Update(
            command.Name ?? snapshot.Name,
            command.JerseyNumber ?? snapshot.JerseyNumber);
        if (update.IsFailure)
            return Result<PlayerMutationResult>.Failure(update.Error!);
        writes.Update(player);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<PlayerMutationResult>.Success(UpdatePlayerCommandHandler.ToResult(player))
            : Result<PlayerMutationResult>.Failure(commit.Error!);
    }
}
