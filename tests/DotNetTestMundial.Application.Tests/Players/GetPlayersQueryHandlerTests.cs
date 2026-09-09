// Responsabilidad del archivo: Verifica validación, normalización y detalle de Queries de jugadores.
// Relación en el sistema: Confirma que sólo especificaciones seguras llegan al repositorio Dapper.
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Common;
using DotNetTestMundial.Application.Players.GetPlayers;
using DotNetTestMundial.Application.Players.Mutations;

namespace DotNetTestMundial.Application.Tests.Players;

public sealed class GetPlayersQueryHandlerTests
{
    [Fact]
    public async Task ValidQuery_NormalizesAndPassesAllFilters()
    {
        var repository = new StubRepository();
        var teamId = Guid.NewGuid();

        var result = await new GetPlayersQueryHandler(repository).HandleAsync(
            new("  ana  ", teamId, true, 2, 5, " JERSEYNUMBER ", " DESC "));

        Assert.True(result.IsSuccess);
        Assert.Equal(new PlayerPageSpecification("ana", teamId, true, 2, 5,
            PlayerSortField.JerseyNumber, PlayerSortDirection.Descending), repository.ReceivedPage);
    }

    [Theory]
    [InlineData(0, 10, "name", "asc", "Players.InvalidPageNumber")]
    [InlineData(1, 0, "name", "asc", "Players.InvalidPageSize")]
    [InlineData(1, 101, "name", "asc", "Players.InvalidPageSize")]
    [InlineData(1, 10, "DROP TABLE", "asc", "Players.InvalidSortBy")]
    [InlineData(1, 10, "name", "sideways", "Players.InvalidSortDirection")]
    public async Task InvalidQuery_DoesNotCallRepository(
        int page, int size, string sort, string direction, string errorCode)
    {
        var repository = new StubRepository();

        var result = await new GetPlayersQueryHandler(repository).HandleAsync(
            new(null, null, null, page, size, sort, direction));

        Assert.Equal(errorCode, result.Error!.Code);
        Assert.Null(repository.ReceivedPage);
    }

    [Fact]
    public async Task EmptyTeamId_IsRejected()
    {
        var result = await new GetPlayersQueryHandler(new StubRepository())
            .HandleAsync(new(TeamId: Guid.Empty));
        Assert.Equal(GetPlayersErrors.InvalidTeamId, result.Error);
    }

    [Fact]
    public async Task Detail_ExistingPlayer_ReturnsProjection()
    {
        var player = new PlayerListItem(Guid.NewGuid(), Guid.NewGuid(), "Ana", 10, true);
        var repository = new StubRepository(player);

        var result = await new GetPlayerByIdQueryHandler(repository).HandleAsync(new(player.Id));

        Assert.True(result.IsSuccess);
        Assert.Same(player, result.Value);
    }

    [Fact]
    public async Task Detail_MissingPlayer_ReturnsNotFound()
    {
        var result = await new GetPlayerByIdQueryHandler(new StubRepository())
            .HandleAsync(new(Guid.NewGuid()));
        Assert.Equal(PlayerMutationErrors.NotFound, result.Error);
    }

    private sealed class StubRepository(PlayerListItem? detail = null) : IPlayerReadRepository
    {
        public PlayerPageSpecification? ReceivedPage { get; private set; }
        public Task<PlayerListItem?> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(detail);
        }

        public Task<PagedResult<PlayerListItem>> GetPageAsync(
            PlayerPageSpecification specification, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ReceivedPage = specification;
            return Task.FromResult(PagedResult<PlayerListItem>.Create([], specification.PageNumber,
                specification.PageSize, 0));
        }
    }
}
