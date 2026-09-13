// Responsabilidad del archivo: Ejecuta el reemplazo completo de los campos editables de un jugador.
// Relación en el sistema: Lee con Dapper, aplica Player.Update y confirma la escritura EF mediante Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Players.Mutations;

public sealed class UpdatePlayerCommandHandler(
    IPlayerReadRepository reads,
    PlayerJerseyValidator jerseyValidator,
    IWriteRepository<Player> writes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePlayerCommand, PlayerMutationResult>
{
    public async Task<Result<PlayerMutationResult>> HandleAsync(
        UpdatePlayerCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<PlayerMutationResult>.Failure(PlayerMutationErrors.NotFound);
        var player = Player.Restore(snapshot.Id, snapshot.TeamId, snapshot.Name,
            snapshot.JerseyNumber, snapshot.IsActive);
        var update = player.Update(command.Name, command.JerseyNumber);
        if (update.IsFailure)
            return Result<PlayerMutationResult>.Failure(update.Error!);
        var jersey = await jerseyValidator.ValidateAsync(
            player.TeamId, player.JerseyNumber, player.Id, cancellationToken);
        if (jersey.IsFailure)
            return Result<PlayerMutationResult>.Failure(jersey.Error!);
        writes.Update(player);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<PlayerMutationResult>.Success(ToResult(player))
            : Result<PlayerMutationResult>.Failure(commit.Error!);
    }

    internal static PlayerMutationResult ToResult(Player player) =>
        new(player.Id, player.TeamId, player.Name, player.JerseyNumber, player.IsActive);
}
