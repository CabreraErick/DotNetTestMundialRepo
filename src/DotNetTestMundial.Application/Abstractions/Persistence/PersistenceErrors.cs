using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Application.Abstractions.Persistence;

public static class PersistenceErrors
{
    public static readonly Error ConcurrentChange = new(
        "Persistence.ConcurrentChange", "The record was changed or removed. Reload it before retrying.", ErrorType.Conflict);
    public static readonly Error ConstraintViolation = new(
        "Persistence.ConstraintViolation", "The change conflicts with existing data or a required relationship.", ErrorType.Conflict);
}
