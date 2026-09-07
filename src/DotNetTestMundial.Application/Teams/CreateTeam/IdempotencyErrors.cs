using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Teams.CreateTeam;

public static class IdempotencyErrors
{
    public static readonly Error KeyRequired = new(
        "Idempotency.KeyRequired", "The Idempotency-Key header is required.", ErrorType.Validation);
    public static readonly Error KeyTooLong = new(
        "Idempotency.KeyTooLong", "Idempotency-Key cannot exceed 200 characters.", ErrorType.Validation);
    public static readonly Error KeyReused = new(
        "Idempotency.KeyReused", "The Idempotency-Key was already used with a different request.", ErrorType.Conflict);
}
