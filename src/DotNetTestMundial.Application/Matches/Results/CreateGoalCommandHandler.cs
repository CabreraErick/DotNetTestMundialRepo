// Responsabilidad del archivo: Orquesta el registro idempotente de un gol válido.
// Relación en el sistema: Lee partido y jugador con Dapper, valida mediante Domain y confirma gol y respuesta con EF y Unit of Work.
using DotNetTestMundial.Application.Abstractions.Messaging;
using DotNetTestMundial.Application.Abstractions.Persistence;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Players.GetPlayers;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Domain.Common;
using DotNetTestMundial.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DotNetTestMundial.Application.Matches.Results;

public sealed class CreateGoalCommandHandler(
    IMatchReadRepository matches,
    IPlayerReadRepository players,
    IWriteRepository<Goal> goalWrites,
    IUnitOfWork unitOfWork,
    IIdempotencyStore idempotencyStore) : ICommandHandler<CreateGoalCommand, CreateGoalOutcome>
{
    private const string Operation = "POST:/api/matches/{matchId}/goals";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<CreateGoalOutcome>> HandleAsync(
        CreateGoalCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return Result<CreateGoalOutcome>.Failure(IdempotencyErrors.KeyRequired);
        var key = command.IdempotencyKey.Trim();
        if (key.Length > 200)
            return Result<CreateGoalOutcome>.Failure(IdempotencyErrors.KeyTooLong);

        // Validate primitive values before reads; Domain repeats the authoritative checks
        // once the persisted player has been reconstructed.
        if (command.MatchId == Guid.Empty)
            return Result<CreateGoalOutcome>.Failure(DomainErrors.MatchRequired);
        if (command.PlayerId == Guid.Empty)
            return Result<CreateGoalOutcome>.Failure(DomainErrors.PlayerRequired);
        if (command.Minute is < 1 or > 120)
            return Result<CreateGoalOutcome>.Failure(DomainErrors.InvalidGoalMinute);

        var requestHash = ComputeRequestHash(command);
        var stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
        if (stored is not null)
            return ReplayOrConflict(stored, requestHash);

        var matchSnapshot = await matches.FindByIdAsync(command.MatchId, cancellationToken);
        if (matchSnapshot is null)
            return Result<CreateGoalOutcome>.Failure(MatchMutationErrors.NotFound);
        var playerSnapshot = await players.FindByIdAsync(command.PlayerId, cancellationToken);
        if (playerSnapshot is null)
            return Result<CreateGoalOutcome>.Failure(MatchResultErrors.PlayerNotFound);

        var player = Restore(playerSnapshot);
        var creation = Goal.Create(command.MatchId, player, command.Minute);
        if (creation.IsFailure)
            return Result<CreateGoalOutcome>.Failure(creation.Error!);
        var goal = creation.Value;
        var match = UpdateMatchCommandHandler.Restore(matchSnapshot);
        var addition = match.AddGoal(goal);
        if (addition.IsFailure)
            return Result<CreateGoalOutcome>.Failure(addition.Error!);

        var responseBody = JsonSerializer.Serialize(new { id = goal.Id }, JsonOptions);
        idempotencyStore.Stage(new StoredIdempotentResponse(
            Operation, key, requestHash, 201, responseBody, goal.Id, DateTime.UtcNow));
        goalWrites.Add(goal);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsSuccess)
            return Result<CreateGoalOutcome>.Success(new(goal.Id, 201, responseBody, false));

        if (commit.Error == PersistenceErrors.ConstraintViolation)
        {
            stored = await idempotencyStore.FindAsync(Operation, key, cancellationToken);
            if (stored is not null)
                return ReplayOrConflict(stored, requestHash);
        }

        return Result<CreateGoalOutcome>.Failure(commit.Error!);
    }

    private static Player Restore(PlayerListItem player) => Player.Restore(
        player.Id, player.TeamId, player.Name, player.JerseyNumber, player.IsActive);

    private static Result<CreateGoalOutcome> ReplayOrConflict(
        StoredIdempotentResponse stored, string requestHash) =>
        stored.RequestHash == requestHash
            ? Result<CreateGoalOutcome>.Success(
                new(stored.ResourceId, stored.StatusCode, stored.ResponseBody, true))
            : Result<CreateGoalOutcome>.Failure(IdempotencyErrors.KeyReused);

    private static string ComputeRequestHash(CreateGoalCommand command)
    {
        var json = JsonSerializer.Serialize(new
        {
            command.MatchId,
            command.PlayerId,
            command.Minute
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
