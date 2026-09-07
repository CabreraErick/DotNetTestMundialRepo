using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DotNetTestMundial.Application.Teams.CreateTeam;

public sealed class CreateTeamCommandHandler(
    IWriteRepository<Team> teams,
    IUnitOfWork unitOfWork,
    IIdempotencyStore idempotencyStore) : ICommandHandler<CreateTeamCommand, CreateTeamOutcome>
{
    private const string Operation = "POST:/api/teams";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<CreateTeamOutcome>> HandleAsync(
        CreateTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Result<CreateTeamOutcome>.Failure(IdempotencyErrors.KeyRequired);
        var key = command.IdempotencyKey.Trim();
        if (key.Length > 200)
            return Result<CreateTeamOutcome>.Failure(IdempotencyErrors.KeyTooLong);

        var creation = Team.Create(command.Name, command.ShortName);
        if (creation.IsFailure)
            return Result<CreateTeamOutcome>.Failure(creation.Error!);

        var requestHash = ComputeRequestHash(command.Name, command.ShortName);
        var stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
        if (stored is not null)
            return ReplayOrConflict(stored, requestHash);

        var team = creation.Value;
        var responseBody = JsonSerializer.Serialize(new { id = team.Id }, JsonOptions);
        idempotencyStore.Stage(new StoredIdempotentResponse(
            Operation, key, requestHash, 201, responseBody, team.Id, DateTime.UtcNow));
        teams.Add(team);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsSuccess)
            return Result<CreateTeamOutcome>.Success(new(team.Id, 201, responseBody, false));

        // A concurrent request may have won the unique-key race. Its committed response is authoritative.
        if (commit.Error == PersistenceErrors.ConstraintViolation)
        {
            stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
            if (stored is not null)
                return ReplayOrConflict(stored, requestHash);
        }

        return Result<CreateTeamOutcome>.Failure(commit.Error!);
    }

    private static Result<CreateTeamOutcome> ReplayOrConflict(StoredIdempotentResponse stored, string requestHash) =>
        stored.RequestHash == requestHash
            ? Result<CreateTeamOutcome>.Success(new(stored.ResourceId, stored.StatusCode, stored.ResponseBody, true))
            : Result<CreateTeamOutcome>.Failure(IdempotencyErrors.KeyReused);

    private static string ComputeRequestHash(string? name, string? shortName)
    {
        var json = JsonSerializer.Serialize(new { name, shortName }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
