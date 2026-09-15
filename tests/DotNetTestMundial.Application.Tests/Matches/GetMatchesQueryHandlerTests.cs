// Responsabilidad del archivo: Verifica filtros, paginación, orden y detalle de Queries de partidos.
// Relación en el sistema: Confirma que Application sólo entrega especificaciones seguras al puerto Dapper.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Matches.Results;
using DotNetTestMundial.Domain.Enums;

namespace DotNetTestMundial.Application.Tests.Matches;

public sealed class GetMatchesQueryHandlerTests
{
    [Fact]
    public async Task ValidQuery_ParsesStatusAndPassesAllFilters()
    {
        var repository = new StubRepository();
        var teamId = Guid.NewGuid();
        var from = new DateTime(2026, 9, 1);
        var to = new DateTime(2026, 9, 30);

        var result = await new GetMatchesQueryHandler(repository).HandleAsync(
            new(teamId, " scheduled ", from, to, 2, 5, " STATUS ", " DESC "));

        Assert.True(result.IsSuccess);
        Assert.Equal(new MatchPageSpecification(teamId, MatchStatus.Scheduled, from, to, 2, 5,
            MatchSortField.Status, MatchSortDirection.Descending), repository.ReceivedPage);
    }

    [Theory]
    [InlineData("invalid", 1, 10, "scheduledAt", "asc", "Matches.InvalidStatus")]
    [InlineData(null, 0, 10, "scheduledAt", "asc", "Matches.InvalidPageNumber")]
    [InlineData(null, 1, 101, "scheduledAt", "asc", "Matches.InvalidPageSize")]
    [InlineData(null, 1, 10, "DROP TABLE", "asc", "Matches.InvalidSortBy")]
    [InlineData(null, 1, 10, "scheduledAt", "sideways", "Matches.InvalidSortDirection")]
    public async Task InvalidQuery_DoesNotCallRepository(
        string? status, int page, int size, string sort, string direction, string errorCode)
    {
        var repository = new StubRepository();

        var result = await new GetMatchesQueryHandler(repository).HandleAsync(
            new(null, status, null, null, page, size, sort, direction));

        Assert.Equal(errorCode, result.Error!.Code);
        Assert.Null(repository.ReceivedPage);
    }

    [Fact]
    public async Task ReversedDateRange_IsRejected()
    {
        var result = await new GetMatchesQueryHandler(new StubRepository()).HandleAsync(new(
            From: new DateTime(2026, 10, 1), To: new DateTime(2026, 9, 1)));
        Assert.Equal(GetMatchesErrors.InvalidDateRange, result.Error);
    }

    [Fact]
    public async Task Detail_ExistingMatch_ReturnsProjection()
    {
        var match = ScheduledMatch();
        var result = await new GetMatchByIdQueryHandler(new StubRepository(match))
            .HandleAsync(new(match.Id));
        Assert.True(result.IsSuccess);
        Assert.Same(match, result.Value);
    }

    [Fact]
    public async Task Detail_MissingMatch_ReturnsNotFound()
    {
        var result = await new GetMatchByIdQueryHandler(new StubRepository())
            .HandleAsync(new(Guid.NewGuid()));
        Assert.Equal(MatchMutationErrors.NotFound, result.Error);
    }

    private static MatchListItem ScheduledMatch() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Local", Guid.NewGuid(), "Visitante",
        DateTime.UtcNow, MatchStatus.Scheduled, null, null, 0);

    private sealed class StubRepository(MatchListItem? detail = null) : IMatchReadRepository
    {
        public Task<bool> HasTeamScheduleConflictAsync(
            Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt,
            Guid? excludingMatchId = null, CancellationToken token = default) =>
            throw new NotSupportedException();
        public MatchPageSpecification? ReceivedPage { get; private set; }

        public Task<MatchListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(detail);
        }

        public Task<PagedResult<MatchListItem>> GetPageAsync(
            MatchPageSpecification specification, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ReceivedPage = specification;
            return Task.FromResult(PagedResult<MatchListItem>.Create(
                [], specification.PageNumber, specification.PageSize, 0));
        }

        public Task<MatchGoalsPage> GetGoalsAsync(
            GoalPageSpecification specification, CancellationToken token = default) => throw new NotSupportedException();

        public Task<MatchStateSnapshot?> FindStateByIdAsync(
            Guid matchId, CancellationToken token = default) => throw new NotSupportedException();
    }
}
