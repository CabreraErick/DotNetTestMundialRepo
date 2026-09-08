// Responsabilidad del archivo: Ejecuta una modificación parcial conservando campos no enviados.
// Relación en el sistema: Combina el snapshot Dapper con el PATCH y reutiliza las invariantes de Team.Update.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Teams.Mutations;

public sealed class PatchTeamCommandHandler(
    ITeamReadRepository reads, IWriteRepository<Team> writes, IUnitOfWork unitOfWork)
    : ICommandHandler<PatchTeamCommand, TeamMutationResult>
{
    public async Task<Result<TeamMutationResult>> HandleAsync(
        PatchTeamCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Name is null && command.ShortName is null)
            return Result<TeamMutationResult>.Failure(TeamMutationErrors.PatchEmpty);
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<TeamMutationResult>.Failure(TeamMutationErrors.NotFound);
        var team = Team.Restore(snapshot.Id, snapshot.Name, snapshot.ShortName);
        var update = team.Update(command.Name ?? snapshot.Name, command.ShortName ?? snapshot.ShortName);
        if (update.IsFailure)
            return Result<TeamMutationResult>.Failure(update.Error!);
        writes.Update(team);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<TeamMutationResult>.Success(new(team.Id, team.Name, team.ShortName))
            : Result<TeamMutationResult>.Failure(commit.Error!);
    }
}
