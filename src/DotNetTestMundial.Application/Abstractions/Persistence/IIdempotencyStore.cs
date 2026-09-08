// Responsabilidad del archivo: Modela el almacenamiento y recuperación de respuestas idempotentes.
// Relación en el sistema: CreateTeamCommandHandler lo usa; Infrastructure combina EF para escritura y Dapper para lectura.
namespace DotNetTestMundial.Application.Abstractions.Persistence;

public interface IIdempotencyStore
{
    Task<StoredIdempotentResponse?> FindAsync(string operation, string key, CancellationToken cancellationToken = default);
    void Stage(StoredIdempotentResponse response);
}

public sealed record StoredIdempotentResponse(
    string Operation,
    string Key,
    string RequestHash,
    int StatusCode,
    string ResponseBody,
    Guid ResourceId,
    DateTime CreatedAt);
