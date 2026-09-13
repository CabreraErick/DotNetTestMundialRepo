// Responsabilidad del archivo: Define el puerto de lectura paginada de equipos.
// Relación en el sistema: GetTeamsQueryHandler depende de él y SqlTeamReadRepository lo implementa con Dapper.
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Teams.GetTeams;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

/// <summary>
/// Read-side port owned by Application. Infrastructure implements it with Dapper so
/// Application can request a page without depending on SQL Server or Dapper packages.
/// </summary>
public interface ITeamReadRepository
{
    /// <summary>Loads the scalar snapshot needed by detail views and write Commands.</summary>
    Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Checks normalized business identifiers before a team write is staged.</summary>
    Task<TeamIdentityConflict> FindIdentityConflictAsync(
        string name,
        string shortName,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TeamListItem>> GetPageAsync(
        TeamPageSpecification specification,
        CancellationToken cancellationToken = default);
}

public sealed record TeamIdentityConflict(bool NameExists, bool ShortNameExists);
