// Responsabilidad del archivo: Obtiene el detalle escalar de un equipo por identificador.
// Relación en el sistema: TeamsController lo invoca y el handler delega la lectura exclusiva al repositorio Dapper.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Teams.Mutations;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Teams.GetTeams;

public sealed record GetTeamByIdQuery(Guid Id);

public sealed class GetTeamByIdQueryHandler(ITeamReadRepository teams)
    : IQueryHandler<GetTeamByIdQuery, TeamListItem>
{
    public async Task<Result<TeamListItem>> HandleAsync(
        GetTeamByIdQuery query, CancellationToken cancellationToken = default)
    {
        var team = await teams.FindByIdAsync(query.Id, cancellationToken);
        return team is null
            ? Result<TeamListItem>.Failure(TeamMutationErrors.NotFound)
            : Result<TeamListItem>.Success(team);
    }
}
