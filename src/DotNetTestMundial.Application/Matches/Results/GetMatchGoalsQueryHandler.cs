// Responsabilidad del archivo: Resuelve la consulta de goles de un partido existente.
// Relación en el sistema: Expone a API una proyección Dapper sin introducir EF Core en el lado de lectura CQRS.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Matches.Results;

public sealed record GetMatchGoalsQuery(Guid MatchId);

public sealed class GetMatchGoalsQueryHandler(IMatchReadRepository matches)
    : IQueryHandler<GetMatchGoalsQuery, IReadOnlyList<GoalListItem>>
{
    public async Task<Result<IReadOnlyList<GoalListItem>>> HandleAsync(
        GetMatchGoalsQuery query, CancellationToken cancellationToken = default)
    {
        if (await matches.FindByIdAsync(query.MatchId, cancellationToken) is null)
            return Result<IReadOnlyList<GoalListItem>>.Failure(MatchMutationErrors.NotFound);

        return Result<IReadOnlyList<GoalListItem>>.Success(
            await matches.GetGoalsAsync(query.MatchId, cancellationToken));
    }
}
