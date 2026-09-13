// Responsabilidad del archivo: Resuelve el detalle de un partido y los nombres de sus equipos.
// Relación en el sistema: MatchesController llama este handler y el repositorio Dapper entrega la proyección sin seguimiento EF.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Matches.GetMatches;

public sealed record GetMatchByIdQuery(Guid Id);

public sealed class GetMatchByIdQueryHandler(IMatchReadRepository matches)
    : IQueryHandler<GetMatchByIdQuery, MatchListItem>
{
    public async Task<Result<MatchListItem>> HandleAsync(
        GetMatchByIdQuery query, CancellationToken cancellationToken = default)
    {
        var match = await matches.FindByIdAsync(query.Id, cancellationToken);
        return match is null
            ? Result<MatchListItem>.Failure(MatchMutationErrors.NotFound)
            : Result<MatchListItem>.Success(match);
    }
}
