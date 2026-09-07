namespace DotNetTestMundial.Infrastructure.Persistence.Idempotency;

internal sealed class IdempotencyRecord
{
    public string Operation { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public string ResponseBody { get; private set; } = string.Empty;
    public Guid ResourceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private IdempotencyRecord() { }

    public IdempotencyRecord(string operation, string key, string requestHash, int statusCode,
        string responseBody, Guid resourceId, DateTime createdAt) =>
        (Operation, Key, RequestHash, StatusCode, ResponseBody, ResourceId, CreatedAt) =
        (operation, key, requestHash, statusCode, responseBody, resourceId, createdAt);
}
