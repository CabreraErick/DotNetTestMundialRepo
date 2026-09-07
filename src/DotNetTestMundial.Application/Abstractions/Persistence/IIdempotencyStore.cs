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
