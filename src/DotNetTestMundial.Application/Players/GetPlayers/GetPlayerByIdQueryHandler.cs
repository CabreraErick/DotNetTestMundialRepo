// Responsabilidad del archivo: Resuelve la consulta de detalle de un jugador por identificador.
// Relación en el sistema: PlayersController lo invoca, Dapper devuelve la proyección y Result expresa un posible 404.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Players.Mutations;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Players.GetPlayers;

public sealed record GetPlayerByIdQuery(Guid Id);

public sealed class GetPlayerByIdQueryHandler(IPlayerReadRepository players)
    : IQueryHandler<GetPlayerByIdQuery, PlayerListItem>
{
    public async Task<Result<PlayerListItem>> HandleAsync(
        GetPlayerByIdQuery query, CancellationToken cancellationToken = default)
    {
        var player = await players.FindByIdAsync(query.Id, cancellationToken);
        return player is null
            ? Result<PlayerListItem>.Failure(PlayerMutationErrors.NotFound)
            : Result<PlayerListItem>.Success(player);
    }
}
