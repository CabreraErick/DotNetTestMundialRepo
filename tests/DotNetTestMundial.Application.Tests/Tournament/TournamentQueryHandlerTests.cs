// Responsabilidad del archivo: Verifica validación, normalización y delegación de posiciones y goleadores.
// Relación en el sistema: Aísla ITournamentReadRepository para garantizar que sólo especificaciones seguras llegan a Dapper.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Tournament.Queries;

namespace DotNetTestMundial.Application.Tests.Tournament;

public sealed class TournamentQueryHandlerTests
{
    [Fact]
    public async Task Standings_ValidQuery_NormalizesAndDelegates()
    {
        var repository = new RecordingRepository();

        var result = await new GetStandingsQueryHandler(repository).HandleAsync(new(
            "  ARG  ", 2, 5, " GoalDifference ", " ASC "));

        Assert.True(result.IsSuccess);
        Assert.Equal(new StandingPageSpecification(
            "ARG", 2, 5, StandingSortField.GoalDifference,
            TournamentSortDirection.Ascending), repository.StandingSpecification);
        Assert.Same(repository.StandingResponse, result.Value);
    }

    [Fact]
    public async Task Standings_Defaults_UseOfficialRankingOrder()
    {
        var repository = new RecordingRepository();

        await new GetStandingsQueryHandler(repository).HandleAsync(new());

        Assert.Equal(new StandingPageSpecification(
            null, 1, 10, StandingSortField.Points,
            TournamentSortDirection.Descending), repository.StandingSpecification);
    }

    [Theory]
    [InlineData(0, 10, "points", "desc", "Tournament.InvalidPageNumber")]
    [InlineData(1, 0, "points", "desc", "Tournament.InvalidPageSize")]
    [InlineData(1, 101, "points", "desc", "Tournament.InvalidPageSize")]
    [InlineData(1, 10, "DROP TABLE", "desc", "Standings.InvalidSortBy")]
    [InlineData(1, 10, "points", "sideways", "Tournament.InvalidSortDirection")]
    public async Task Standings_InvalidQuery_DoesNotCallRepository(
        int page, int size, string sort, string direction, string errorCode)
    {
        var repository = new RecordingRepository();

        var result = await new GetStandingsQueryHandler(repository).HandleAsync(
            new(null, page, size, sort, direction));

        Assert.Equal(errorCode, result.Error!.Code);
        Assert.Null(repository.StandingSpecification);
    }

    [Fact]
    public async Task Scorers_ValidQuery_PreservesTeamAndNormalizesSearch()
    {
        var repository = new RecordingRepository();
        var teamId = Guid.NewGuid();

        var result = await new GetScorersQueryHandler(repository).HandleAsync(new(
            "  scorer  ", teamId, 3, 4, " TeamName ", " DESC "));

        Assert.True(result.IsSuccess);
        Assert.Equal(new ScorerPageSpecification(
            "scorer", teamId, 3, 4, ScorerSortField.TeamName,
            TournamentSortDirection.Descending), repository.ScorerSpecification);
        Assert.Same(repository.ScorerResponse, result.Value);
    }

    [Theory]
    [InlineData("invalid", "desc", "Scorers.InvalidSortBy")]
    [InlineData("goals", "invalid", "Tournament.InvalidSortDirection")]
    public async Task Scorers_InvalidOrder_DoesNotCallRepository(
        string sort, string direction, string errorCode)
    {
        var repository = new RecordingRepository();

        var result = await new GetScorersQueryHandler(repository).HandleAsync(new(
            SortBy: sort, SortDirection: direction));

        Assert.Equal(errorCode, result.Error!.Code);
        Assert.Null(repository.ScorerSpecification);
    }

    [Fact]
    public async Task Queries_ForwardCancellationToken()
    {
        var repository = new RecordingRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GetStandingsQueryHandler(repository).HandleAsync(new(), cancellation.Token));
    }

    private sealed class RecordingRepository : ITournamentReadRepository
    {
        public StandingPageSpecification? StandingSpecification { get; private set; }
        public ScorerPageSpecification? ScorerSpecification { get; private set; }
        public PagedResult<StandingListItem> StandingResponse { get; } =
            PagedResult<StandingListItem>.Create([], 2, 5, 4);
        public PagedResult<ScorerListItem> ScorerResponse { get; } =
            PagedResult<ScorerListItem>.Create([], 3, 4, 7);

        public Task<PagedResult<StandingListItem>> GetStandingsAsync(
            StandingPageSpecification specification,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StandingSpecification = specification;
            return Task.FromResult(StandingResponse);
        }

        public Task<PagedResult<ScorerListItem>> GetScorersAsync(
            ScorerPageSpecification specification,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScorerSpecification = specification;
            return Task.FromResult(ScorerResponse);
        }
    }
}
