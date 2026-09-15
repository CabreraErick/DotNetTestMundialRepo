// Responsabilidad del archivo: Define el puerto de lectura para calendario, goles y estado completo de partidos.
// Relación en el sistema: Los handlers dependen de este contrato y Infrastructure lo implementa exclusivamente con Dapper.
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Results;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface IMatchReadRepository
{
    Task<MatchListItem?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Detects whether either participant already has a non-cancelled match on the scheduled day.</summary>
    Task<bool> HasTeamScheduleConflictAsync(
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime scheduledAt,
        Guid? excludingMatchId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MatchListItem>> GetPageAsync(
        MatchPageSpecification specification,
        CancellationToken cancellationToken = default);

    Task<MatchGoalsPage> GetGoalsAsync(
        GoalPageSpecification specification, CancellationToken cancellationToken = default);

    Task<MatchStateSnapshot?> FindStateByIdAsync(
        Guid matchId, CancellationToken cancellationToken = default);
}
