// Responsabilidad del archivo: valida la reserva permanente del dorsal dentro de un equipo.
// Relación en el sistema: creación y edición consultan por Dapper antes de persistir al jugador con EF Core.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Players.Mutations;

public sealed class PlayerJerseyValidator(IPlayerReadRepository players)
{
    public async Task<Result> ValidateAsync(
        Guid teamId,
        int jerseyNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default) =>
        await players.IsJerseyNumberInUseAsync(
            teamId, jerseyNumber, excludingId, cancellationToken)
            ? Result.Failure(PlayerMutationErrors.JerseyNumberAlreadyAssigned)
            : Result.Success();
}
