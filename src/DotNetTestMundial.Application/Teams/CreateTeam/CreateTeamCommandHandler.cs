using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;

namespace DotNetTestMundial.Application.Teams.CreateTeam;

public sealed class CreateTeamCommandHandler(
    IWriteRepository<Team> teams,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTeamCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var creation = Team.Create(command.Name, command.ShortName);
        if (creation.IsFailure)
            return Result<Guid>.Failure(creation.Error!);

        var team = creation.Value;
        teams.Add(team);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess
            ? Result<Guid>.Success(team.Id)
            : Result<Guid>.Failure(commit.Error!);
    }
}
