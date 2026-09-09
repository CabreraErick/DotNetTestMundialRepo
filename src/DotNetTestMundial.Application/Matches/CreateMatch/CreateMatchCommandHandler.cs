// Responsabilidad del archivo: Orquesta programación idempotente y commit transaccional de un partido.
// Relación en el sistema: Valida equipos con Dapper, crea Match en Domain y persiste partido y respuesta con EF y Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DotNetTestMundial.Application.Matches.CreateMatch;

public sealed class CreateMatchCommandHandler(
    MatchTeamValidator teamValidator,
    IWriteRepository<Match> matches,
    IUnitOfWork unitOfWork,
    IIdempotencyStore idempotencyStore) : ICommandHandler<CreateMatchCommand, CreateMatchOutcome>
{
    private const string Operation = "POST:/api/matches";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<CreateMatchOutcome>> HandleAsync(
        CreateMatchCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Result<CreateMatchOutcome>.Failure(IdempotencyErrors.KeyRequired);
        var key = command.IdempotencyKey.Trim();
        if (key.Length > 200)
            return Result<CreateMatchOutcome>.Failure(IdempotencyErrors.KeyTooLong);

        var creation = Match.Create(command.HomeTeamId, command.AwayTeamId, command.ScheduledAt);
        if (creation.IsFailure)
            return Result<CreateMatchOutcome>.Failure(creation.Error!);

        var requestHash = ComputeRequestHash(command);
        var stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
        if (stored is not null)
            return ReplayOrConflict(stored, requestHash);

        var teamValidation = await teamValidator.ValidateAsync(
            command.HomeTeamId, command.AwayTeamId, cancellationToken);
        if (teamValidation.IsFailure)
            return Result<CreateMatchOutcome>.Failure(teamValidation.Error!);

        var match = creation.Value;
        var responseBody = JsonSerializer.Serialize(new { id = match.Id }, JsonOptions);
        idempotencyStore.Stage(new StoredIdempotentResponse(
            Operation, key, requestHash, 201, responseBody, match.Id, DateTime.UtcNow));
        matches.Add(match);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsSuccess)
            return Result<CreateMatchOutcome>.Success(new(match.Id, 201, responseBody, false));

        if (commit.Error == PersistenceErrors.ConstraintViolation)
        {
            stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
            if (stored is not null)
                return ReplayOrConflict(stored, requestHash);
        }

        return Result<CreateMatchOutcome>.Failure(commit.Error!);
    }

    private static Result<CreateMatchOutcome> ReplayOrConflict(
        StoredIdempotentResponse stored, string requestHash) =>
        stored.RequestHash == requestHash
            ? Result<CreateMatchOutcome>.Success(
                new(stored.ResourceId, stored.StatusCode, stored.ResponseBody, true))
            : Result<CreateMatchOutcome>.Failure(IdempotencyErrors.KeyReused);

    private static string ComputeRequestHash(CreateMatchCommand command)
    {
        var json = JsonSerializer.Serialize(new
        {
            command.HomeTeamId,
            command.AwayTeamId,
            command.ScheduledAt
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
