// Responsabilidad del archivo: Define el puerto de lectura para posiciones y goleadores del torneo.
// Relación en el sistema: Los Query handlers dependen de este contrato e Infrastructure lo implementa sólo con Dapper.
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Tournament.Queries;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface ITournamentReadRepository
{
    Task<PagedResult<StandingListItem>> GetStandingsAsync(
        StandingPageSpecification specification,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ScorerListItem>> GetScorersAsync(
        ScorerPageSpecification specification,
        CancellationToken cancellationToken = default);
}
