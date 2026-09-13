// Responsabilidad del archivo: Define el puerto de lectura de jugadores para detalle y colecciones paginadas.
// Relación en el sistema: Los Query y Command handlers dependen de él; Infrastructure lo implementa exclusivamente con Dapper.
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Players.GetPlayers;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface IPlayerReadRepository
{
    Task<PlayerListItem?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Reserves a jersey number for one player in a team, regardless of active state.</summary>
    Task<bool> IsJerseyNumberInUseAsync(
        Guid teamId,
        int jerseyNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PlayerListItem>> GetPageAsync(
        PlayerPageSpecification specification,
        CancellationToken cancellationToken = default);
}
