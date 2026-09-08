// Responsabilidad del archivo: Verifica validación y normalización de la Query de equipos.
// Relación en el sistema: Confirma que sólo especificaciones seguras llegan al repositorio Dapper.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Teams.GetTeams;

namespace DotNetTestMundial.Application.Tests.Teams;

/// <summary>
/// Verifies that Application rejects unsafe pagination/order values before Infrastructure
/// is called and passes only normalized specifications to the Dapper adapter.
/// </summary>
public sealed class GetTeamsQueryHandlerTests
{
    [Fact]
    public async Task ValidQuery_NormalizesValuesAndReturnsRepositoryPage()
    {
        var repository = new RecordingReadRepository();
        var result = await new GetTeamsQueryHandler(repository).HandleAsync(
            new("  arg  ", 2, 5, " SHORTNAME ", " DESC "));

        Assert.True(result.IsSuccess);
        var specification = Assert.IsType<TeamPageSpecification>(repository.Received);
        Assert.Equal("arg", specification.Search);
        Assert.Equal(2, specification.PageNumber);
        Assert.Equal(5, specification.PageSize);
        Assert.Equal(TeamSortField.ShortName, specification.SortField);
        Assert.Equal(QuerySortDirection.Descending, specification.SortDirection);
        Assert.Same(repository.Response, result.Value);
    }

    [Fact]
    public async Task Defaults_RequestFirstPageOrderedByNameAscending()
    {
        var repository = new RecordingReadRepository();
        await new GetTeamsQueryHandler(repository).HandleAsync(new());
        Assert.Equal(new TeamPageSpecification(null, 1, 10, TeamSortField.Name,
            QuerySortDirection.Ascending), repository.Received);
    }

    [Theory]
    [InlineData(0, 10, "name", "asc", "Teams.InvalidPageNumber")]
    [InlineData(1, 0, "name", "asc", "Teams.InvalidPageSize")]
    [InlineData(1, 101, "name", "asc", "Teams.InvalidPageSize")]
    [InlineData(1, 10, "DROP TABLE Teams", "asc", "Teams.InvalidSortBy")]
    [InlineData(1, 10, "name", "sideways", "Teams.InvalidSortDirection")]
    public async Task InvalidQuery_ReturnsValidationWithoutCallingRepository(
        int pageNumber, int pageSize, string sortBy, string direction, string errorCode)
    {
        var repository = new RecordingReadRepository();
        var result = await new GetTeamsQueryHandler(repository).HandleAsync(
            new(null, pageNumber, pageSize, sortBy, direction));
        Assert.Equal(errorCode, result.Error!.Code);
        Assert.Null(repository.Received);
    }

    [Fact]
    public async Task RepositoryReceivesCancellationToken()
    {
        var repository = new RecordingReadRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GetTeamsQueryHandler(repository).HandleAsync(new(), cancellation.Token));
    }

    [Fact]
    public void PagedResult_ComputesTotalPagesIncludingPartialLastPage()
    {
        var page = PagedResult<TeamListItem>.Create([], 2, 10, 21);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(21, page.TotalRecords);
    }

    private sealed class RecordingReadRepository : ITeamReadRepository
    {
        public TeamPageSpecification? Received { get; private set; }
        public PagedResult<TeamListItem> Response { get; } = PagedResult<TeamListItem>.Create(
            [new(Guid.NewGuid(), "Argentina", "ARG")], 2, 5, 6);

        public Task<PagedResult<TeamListItem>> GetPageAsync(
            TeamPageSpecification specification, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Received = specification;
            return Task.FromResult(Response);
        }

        public Task<TeamListItem?> FindByIdAsync(
            Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TeamListItem?>(null);
        }
    }
}
