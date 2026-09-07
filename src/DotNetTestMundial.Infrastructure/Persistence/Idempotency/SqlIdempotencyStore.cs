using Dapper;
using DotNetTestMundial.Application.Abstractions.Persistence;
using Microsoft.Data.SqlClient;

namespace DotNetTestMundial.Infrastructure.Persistence.Idempotency;

internal sealed class SqlIdempotencyStore(TournamentDbContext context, string connectionString) : IIdempotencyStore
{
    public void Stage(StoredIdempotentResponse response) => context.Set<IdempotencyRecord>().Add(new(
        response.Operation, response.Key, response.RequestHash, response.StatusCode,
        response.ResponseBody, response.ResourceId, response.CreatedAt));

    public async Task<StoredIdempotentResponse?> FindAsync(
        string operation, string key, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Operation, [Key], RequestHash, StatusCode, ResponseBody, ResourceId, CreatedAt
            FROM dbo.IdempotencyRecords
            WHERE Operation = @operation AND [Key] = @key;
            """;
        await using var connection = new SqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<StoredIdempotentResponse>(
            new CommandDefinition(sql, new { operation, key }, cancellationToken: cancellationToken));
    }
}
