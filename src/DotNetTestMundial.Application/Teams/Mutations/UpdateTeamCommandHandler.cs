// Responsabilidad del archivo: Ejecuta el reemplazo completo de los datos editables de un equipo.
// Relación en el sistema: Lee el estado con Dapper, aplica Team.Update y confirma mediante Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Teams.Mutations;

public sealed class UpdateTeamCommandHandler(
    ITeamReadRepository reads,
    TeamIdentityValidator identityValidator,
    IWriteRepository<Team> writes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTeamCommand, TeamMutationResult>
{
    public async Task<Result<TeamMutationResult>> HandleAsync(
        UpdateTeamCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<TeamMutationResult>.Failure(TeamMutationErrors.NotFound);
        var team = Team.Restore(snapshot.Id, snapshot.Name, snapshot.ShortName);
        var update = team.Update(command.Name, command.ShortName);
        if (update.IsFailure)
            return Result<TeamMutationResult>.Failure(update.Error!);
        var uniqueness = await identityValidator.ValidateAsync(
            team.Name, team.ShortName, team.Id, cancellationToken);
        if (uniqueness.IsFailure)
            return Result<TeamMutationResult>.Failure(uniqueness.Error!);
        writes.Update(team);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<TeamMutationResult>.Success(new(team.Id, team.Name, team.ShortName))
            : Result<TeamMutationResult>.Failure(commit.Error!);
    }
}
