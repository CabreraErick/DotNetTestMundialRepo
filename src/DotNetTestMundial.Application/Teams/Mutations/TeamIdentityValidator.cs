// Responsabilidad del archivo: valida que nombre y abreviatura identifiquen un único equipo.
// Relación en el sistema: los Commands consultan por Dapper antes de que EF Core prepare la escritura.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Teams.Mutations;

public sealed class TeamIdentityValidator(ITeamReadRepository teams)
{
    public async Task<Result> ValidateAsync(
        string name,
        string shortName,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var conflict = await teams.FindIdentityConflictAsync(
            name, shortName, excludingId, cancellationToken);
        if (conflict.NameExists)
            return Result.Failure(TeamMutationErrors.NameAlreadyExists);
        if (conflict.ShortNameExists)
            return Result.Failure(TeamMutationErrors.ShortNameAlreadyExists);
        return Result.Success();
    }
}
