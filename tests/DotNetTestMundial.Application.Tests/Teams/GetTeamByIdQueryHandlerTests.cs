// Responsabilidad del archivo: Verifica la consulta de un equipo por su identificador.
// Relación en el sistema: Confirma que el Query Handler delega la lectura en Dapper y expresa ausencia con Result.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Application.Teams.Mutations;

namespace DotNetTestMundial.Application.Tests.Teams;

public sealed class GetTeamByIdQueryHandlerTests
{
    [Fact]
    public async Task ExistingTeam_ReturnsDapperProjection()
    {
        var team = new TeamListItem(Guid.NewGuid(), "Argentina", "ARG");
        var repository = new StubReadRepository(team);

        var result = await new GetTeamByIdQueryHandler(repository)
            .HandleAsync(new(team.Id));

        Assert.True(result.IsSuccess);
        Assert.Same(team, result.Value);
        Assert.Equal(team.Id, repository.ReceivedId);
    }

    [Fact]
    public async Task MissingTeam_ReturnsNotFound()
    {
        var repository = new StubReadRepository(null);

        var result = await new GetTeamByIdQueryHandler(repository)
            .HandleAsync(new(Guid.NewGuid()));

        Assert.Equal(TeamMutationErrors.NotFound, result.Error);
    }

    private sealed class StubReadRepository(TeamListItem? response) : ITeamReadRepository
    {
        public Task<TeamIdentityConflict> FindIdentityConflictAsync(
            string name, string shortName, Guid? excludingId = null,
            CancellationToken token = default) => throw new NotSupportedException();
        public Guid ReceivedId { get; private set; }
        public Task<TeamListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ReceivedId = id;
            return Task.FromResult(response);
        }

        public Task<PagedResult<TeamListItem>> GetPageAsync(
            TeamPageSpecification specification, CancellationToken token = default) =>
            throw new NotSupportedException();
    }
}
