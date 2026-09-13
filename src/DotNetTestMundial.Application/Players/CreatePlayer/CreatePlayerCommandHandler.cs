// Responsabilidad del archivo: Orquesta la creación idempotente de jugadores y su commit transaccional.
// Relación en el sistema: Valida equipo con Dapper, crea Player en Domain y persiste jugador y respuesta con EF y Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Players.Mutations;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DotNetTestMundial.Application.Players.CreatePlayer;

public sealed class CreatePlayerCommandHandler(
    ITeamReadRepository teams,
    PlayerJerseyValidator jerseyValidator,
    IWriteRepository<Player> players,
    IUnitOfWork unitOfWork,
    IIdempotencyStore idempotencyStore) : ICommandHandler<CreatePlayerCommand, CreatePlayerOutcome>
{
    private const string Operation = "POST:/api/players";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<CreatePlayerOutcome>> HandleAsync(
        CreatePlayerCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Result<CreatePlayerOutcome>.Failure(IdempotencyErrors.KeyRequired);
        var key = command.IdempotencyKey.Trim();
        if (key.Length > 200)
            return Result<CreatePlayerOutcome>.Failure(IdempotencyErrors.KeyTooLong);

        var creation = Player.Create(command.TeamId, command.Name, command.JerseyNumber);
        if (creation.IsFailure)
            return Result<CreatePlayerOutcome>.Failure(creation.Error!);

        var requestHash = ComputeRequestHash(command.TeamId, command.Name, command.JerseyNumber);
        var stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
        if (stored is not null)
            return ReplayOrConflict(stored, requestHash);

        if (await teams.FindByIdAsync(command.TeamId, cancellationToken) is null)
            return Result<CreatePlayerOutcome>.Failure(PlayerMutationErrors.TeamNotFound);

        var player = creation.Value;
        var jersey = await jerseyValidator.ValidateAsync(
            player.TeamId, player.JerseyNumber, cancellationToken: cancellationToken);
        if (jersey.IsFailure)
            return Result<CreatePlayerOutcome>.Failure(jersey.Error!);
        var responseBody = JsonSerializer.Serialize(new { id = player.Id }, JsonOptions);
        idempotencyStore.Stage(new StoredIdempotentResponse(
            Operation, key, requestHash, 201, responseBody, player.Id, DateTime.UtcNow));
        players.Add(player);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsSuccess)
            return Result<CreatePlayerOutcome>.Success(new(player.Id, 201, responseBody, false));

        if (commit.Error == PersistenceErrors.ConstraintViolation)
        {
            stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
            if (stored is not null)
                return ReplayOrConflict(stored, requestHash);
        }

        return Result<CreatePlayerOutcome>.Failure(commit.Error!);
    }

    private static Result<CreatePlayerOutcome> ReplayOrConflict(
        StoredIdempotentResponse stored, string requestHash) =>
        stored.RequestHash == requestHash
            ? Result<CreatePlayerOutcome>.Success(
                new(stored.ResourceId, stored.StatusCode, stored.ResponseBody, true))
            : Result<CreatePlayerOutcome>.Failure(IdempotencyErrors.KeyReused);

    private static string ComputeRequestHash(Guid teamId, string? name, int jerseyNumber)
    {
        var json = JsonSerializer.Serialize(new { teamId, name, jerseyNumber }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
