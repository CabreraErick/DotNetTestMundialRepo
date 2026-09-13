// Responsabilidad del archivo: Elimina un equipo existente mediante un Command transaccional.
// Relación en el sistema: Dapper comprueba existencia, EF prepara DELETE y Unit of Work traduce conflictos de relaciones.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Teams.Mutations;

public sealed class DeleteTeamCommandHandler(
    ITeamReadRepository reads, IWriteRepository<Team> writes, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTeamCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        DeleteTeamCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await reads.FindByIdAsync(command.Id, cancellationToken);
        if (snapshot is null)
            return Result<Guid>.Failure(TeamMutationErrors.NotFound);
        var team = Team.Restore(snapshot.Id, snapshot.Name, snapshot.ShortName);
        writes.Remove(team);
        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess ? Result<Guid>.Success(team.Id) : Result<Guid>.Failure(commit.Error!);
    }
}
